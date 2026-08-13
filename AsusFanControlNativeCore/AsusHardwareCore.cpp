#include "pch.h"
#include "AsusHardwareCore.h"

#include <algorithm>
#include <cmath>
#include <chrono>
#include <sstream>
#include <stdexcept>
#include <thread>

namespace
{
std::wstring Utf8ToWide(std::string const& value)
{
    if (value.empty())
    {
        return L"Unknown error.";
    }

    const int sourceLength = static_cast<int>(value.size());
    const int required = MultiByteToWideChar(CP_UTF8, 0, value.c_str(), sourceLength, nullptr, 0);
    if (required <= 0)
    {
        return std::wstring(value.begin(), value.end());
    }

    std::wstring result(static_cast<std::size_t>(required), L'\0');
    const int written = MultiByteToWideChar(CP_UTF8, 0, value.c_str(), sourceLength, result.data(), required);
    if (written <= 0)
    {
        return std::wstring(value.begin(), value.end());
    }

    return result;
}

std::wstring JoinErrors(std::vector<std::wstring> const& errors)
{
    std::wstringstream stream;
    for (std::size_t index = 0; index < errors.size(); ++index)
    {
        if (index > 0)
        {
            stream << L" ";
        }

        stream << errors[index];
    }

    return stream.str();
}

short PercentToDuty(int percent)
{
    const int clamped = std::clamp(percent, 0, 100);
    const double scaled = static_cast<double>(clamped) / 100.0 * 255.0;
    return static_cast<short>(std::lround(scaled));
}
}

namespace AsusFanControlNativeCore
{
OperationResult AsusHardwareCore::EnsureLoaded()
{
    std::wstring errorMessage;
    if (bindings_.IsLoaded())
    {
        return {};
    }

    if (!bindings_.Load(errorMessage))
    {
        return OperationResult{ false, errorMessage };
    }

    try
    {
        bindings_.InitializeWinIo();
    }
    catch (std::exception const& ex)
    {
        bindings_.Unload();
        return OperationResult{ false, Utf8ToWide(ex.what()) };
    }
    catch (...)
    {
        bindings_.Unload();
        return OperationResult{ false, L"Unknown ASUS hardware initialization failure." };
    }

    return {};
}

HardwareSnapshot AsusHardwareCore::ReadSnapshot()
{
    HardwareSnapshot snapshot;

    const OperationResult loadResult = EnsureLoaded();
    if (!loadResult.Success)
    {
        snapshot.Ready = false;
        snapshot.StatusText = L"ASUS interface unavailable";
        snapshot.ErrorMessage = loadResult.ErrorMessage;
        return snapshot;
    }

    std::vector<std::wstring> errors;

    try
    {
        const int fanCount = bindings_.FanCount();
        if (fanCount <= 0)
        {
            errors.emplace_back(L"No fan channels reported.");
        }
        else
        {
            snapshot.FanSpeeds.reserve(static_cast<std::size_t>(fanCount));
            for (int index = 0; index < fanCount; ++index)
            {
                bindings_.SetFanIndex(static_cast<std::uint8_t>(index));
                snapshot.FanSpeeds.push_back(bindings_.FanRpm());
                std::this_thread::sleep_for(std::chrono::milliseconds(10));
            }
        }
    }
    catch (std::exception const& ex)
    {
        errors.emplace_back(L"RPM: " + Utf8ToWide(ex.what()));
    }
    catch (...)
    {
        errors.emplace_back(L"RPM: Unknown failure.");
    }

    try
    {
        const std::uint64_t cpuTemperature = bindings_.ReadCpuTemperature();
        if (cpuTemperature > 0 && cpuTemperature <= 150)
        {
            snapshot.CpuTemperature = static_cast<int>(cpuTemperature);
        }
    }
    catch (std::exception const& ex)
    {
        UNREFERENCED_PARAMETER(ex);
    }
    catch (...)
    {
    }

    snapshot.Ready = errors.empty();
    snapshot.StatusText = snapshot.Ready ? L"ASUS interface ready" : L"ASUS interface issue";
    snapshot.ErrorMessage = errors.empty() ? L"" : JoinErrors(errors);
    return snapshot;
}

OperationResult AsusHardwareCore::ApplySpeedPercent(int percent)
{
    return ApplySpeedPercentInternal(percent);
}

OperationResult AsusHardwareCore::DisableControl()
{
    return ApplySpeedPercentInternal(0);
}

OperationResult AsusHardwareCore::ApplySpeedPercentInternal(int percent)
{
    const OperationResult loadResult = EnsureLoaded();
    if (!loadResult.Success)
    {
        return loadResult;
    }

    std::vector<std::uint8_t> appliedFans;
    auto rollbackAppliedFans = [&]()
    {
        try
        {
            for (std::uint8_t fanIndex : appliedFans)
            {
                bindings_.SetFanIndex(fanIndex);
                bindings_.SetFanTestMode(false);
                bindings_.SetFanPwmDuty(0);
            }
        }
        catch (...)
        {
        }
    };

    try
    {
        const int fanCount = bindings_.FanCount();
        if (fanCount <= 0)
        {
            return OperationResult{ false, L"No fan channels reported." };
        }

        const bool enableTestMode = percent > 0;
        const short duty = enableTestMode ? PercentToDuty(percent) : 0;
        appliedFans.reserve(static_cast<std::size_t>(fanCount));

        for (int index = 0; index < fanCount; ++index)
        {
            const std::uint8_t fanIndex = static_cast<std::uint8_t>(index);
            bindings_.SetFanIndex(fanIndex);
            bindings_.SetFanTestMode(enableTestMode);
            bindings_.SetFanPwmDuty(duty);
            appliedFans.push_back(fanIndex);
            std::this_thread::sleep_for(std::chrono::milliseconds(20));
        }
    }
    catch (std::exception const& ex)
    {
        rollbackAppliedFans();
        return OperationResult{ false, Utf8ToWide(ex.what()) };
    }
    catch (...)
    {
        rollbackAppliedFans();
        return OperationResult{ false, L"Unknown ASUS hardware write failure." };
    }

    return {};
}
}

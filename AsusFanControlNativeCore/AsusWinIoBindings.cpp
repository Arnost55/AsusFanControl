#include "pch.h"
#include "AsusWinIoBindings.h"

#include <stdexcept>

namespace
{
template <typename T>
T ResolveExport(HMODULE module, char const* name)
{
    return reinterpret_cast<T>(GetProcAddress(module, name));
}
}

namespace AsusFanControlNativeCore
{
AsusWinIoBindings::~AsusWinIoBindings()
{
    Unload();
}

bool AsusWinIoBindings::IsLoaded() const noexcept
{
    return module_ != nullptr;
}

bool AsusWinIoBindings::Load(std::wstring& errorMessage)
{
    if (module_ != nullptr)
    {
        return true;
    }

    module_ = LoadLibraryW(L"AsusWinIO64.dll");
    if (module_ == nullptr)
    {
        errorMessage = L"Unable to load AsusWinIO64.dll. " + FormatWin32Error(GetLastError());
        return false;
    }

    if (!ResolveFunctions(errorMessage))
    {
        FreeLibrary(module_);
        module_ = nullptr;
        return false;
    }

    initialized_ = false;
    return true;
}

void AsusWinIoBindings::Unload()
{
    try
    {
        ShutdownWinIo();
    }
    catch (...)
    {
        // Best effort cleanup only.
    }

    if (module_ != nullptr)
    {
        FreeLibrary(module_);
        module_ = nullptr;
    }

    initializeWinIo_ = nullptr;
    shutdownWinIo_ = nullptr;
    fanCounts_ = nullptr;
    setFanIndex_ = nullptr;
    fanRpm_ = nullptr;
    setFanTestMode_ = nullptr;
    setFanPwmDuty_ = nullptr;
    readCpuTemperature_ = nullptr;
}

void AsusWinIoBindings::EnsureLoaded() const
{
    if (module_ == nullptr)
    {
        throw std::runtime_error("AsusWinIoBindings was not loaded.");
    }
}

void AsusWinIoBindings::InitializeWinIo()
{
    EnsureLoaded();

    if (!initialized_ && initializeWinIo_ != nullptr)
    {
        initializeWinIo_();
        initialized_ = true;
    }
}

void AsusWinIoBindings::ShutdownWinIo()
{
    if (module_ == nullptr)
    {
        return;
    }

    if (initialized_ && shutdownWinIo_ != nullptr)
    {
        shutdownWinIo_();
    }

    initialized_ = false;
}

int AsusWinIoBindings::FanCount() const
{
    EnsureLoaded();
    if (fanCounts_ == nullptr)
    {
        throw std::runtime_error("Missing export: HealthyTable_FanCounts");
    }

    return fanCounts_();
}

void AsusWinIoBindings::SetFanIndex(std::uint8_t index) const
{
    EnsureLoaded();
    if (setFanIndex_ == nullptr)
    {
        throw std::runtime_error("Missing export: HealthyTable_SetFanIndex");
    }

    setFanIndex_(index);
}

int AsusWinIoBindings::FanRpm() const
{
    EnsureLoaded();
    if (fanRpm_ == nullptr)
    {
        throw std::runtime_error("Missing export: HealthyTable_FanRPM");
    }

    return fanRpm_();
}

void AsusWinIoBindings::SetFanTestMode(bool enabled) const
{
    EnsureLoaded();
    if (setFanTestMode_ == nullptr)
    {
        throw std::runtime_error("Missing export: HealthyTable_SetFanTestMode");
    }

    const char mode = enabled ? 0x01 : 0x00;
    setFanTestMode_(mode);
}

void AsusWinIoBindings::SetFanPwmDuty(short duty) const
{
    EnsureLoaded();
    if (setFanPwmDuty_ == nullptr)
    {
        throw std::runtime_error("Missing export: HealthyTable_SetFanPwmDuty");
    }

    setFanPwmDuty_(duty);
}

std::uint64_t AsusWinIoBindings::ReadCpuTemperature() const
{
    EnsureLoaded();
    if (readCpuTemperature_ == nullptr)
    {
        throw std::runtime_error("Missing export: Thermal_Read_Cpu_Temperature");
    }

    return readCpuTemperature_();
}

bool AsusWinIoBindings::ResolveFunctions(std::wstring& errorMessage)
{
    initializeWinIo_ = ResolveExport<InitializeWinIoFn>(module_, "InitializeWinIo");
    if (initializeWinIo_ == nullptr)
    {
        errorMessage = L"Missing export: InitializeWinIo";
        return false;
    }

    shutdownWinIo_ = ResolveExport<ShutdownWinIoFn>(module_, "ShutdownWinIo");
    if (shutdownWinIo_ == nullptr)
    {
        errorMessage = L"Missing export: ShutdownWinIo";
        return false;
    }

    fanCounts_ = ResolveExport<FanCountsFn>(module_, "HealthyTable_FanCounts");
    if (fanCounts_ == nullptr)
    {
        errorMessage = L"Missing export: HealthyTable_FanCounts";
        return false;
    }

    setFanIndex_ = ResolveExport<SetFanIndexFn>(module_, "HealthyTable_SetFanIndex");
    if (setFanIndex_ == nullptr)
    {
        errorMessage = L"Missing export: HealthyTable_SetFanIndex";
        return false;
    }

    fanRpm_ = ResolveExport<FanRpmFn>(module_, "HealthyTable_FanRPM");
    if (fanRpm_ == nullptr)
    {
        errorMessage = L"Missing export: HealthyTable_FanRPM";
        return false;
    }

    setFanTestMode_ = ResolveExport<SetFanTestModeFn>(module_, "HealthyTable_SetFanTestMode");
    if (setFanTestMode_ == nullptr)
    {
        errorMessage = L"Missing export: HealthyTable_SetFanTestMode";
        return false;
    }

    setFanPwmDuty_ = ResolveExport<SetFanPwmDutyFn>(module_, "HealthyTable_SetFanPwmDuty");
    if (setFanPwmDuty_ == nullptr)
    {
        errorMessage = L"Missing export: HealthyTable_SetFanPwmDuty";
        return false;
    }

    readCpuTemperature_ = ResolveExport<ReadCpuTemperatureFn>(module_, "Thermal_Read_Cpu_Temperature");
    if (readCpuTemperature_ == nullptr)
    {
        errorMessage = L"Missing export: Thermal_Read_Cpu_Temperature";
        return false;
    }

    return true;
}

std::wstring AsusWinIoBindings::FormatWin32Error(DWORD errorCode)
{
    if (errorCode == 0)
    {
        return L"";
    }

    LPWSTR buffer = nullptr;
    const DWORD length = FormatMessageW(
        FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
        nullptr,
        errorCode,
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        reinterpret_cast<LPWSTR>(&buffer),
        0,
        nullptr);

    std::wstring message;
    if (length > 0 && buffer != nullptr)
    {
        message.assign(buffer, length);
        LocalFree(buffer);
    }

    return message;
}
}

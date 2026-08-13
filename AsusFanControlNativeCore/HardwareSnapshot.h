#pragma once

#include <optional>
#include <string>
#include <vector>

namespace AsusFanControlNativeCore
{
struct HardwareSnapshot
{
    bool Ready{ false };
    std::wstring StatusText{ L"ASUS interface unavailable" };
    std::wstring ErrorMessage;
    std::vector<int> FanSpeeds;
    std::optional<int> CpuTemperature;

    int FanCount() const noexcept
    {
        return static_cast<int>(FanSpeeds.size());
    }
};

struct OperationResult
{
    bool Success{ true };
    std::wstring ErrorMessage;
};
}

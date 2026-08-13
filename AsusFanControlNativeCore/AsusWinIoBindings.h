#pragma once

#include <cstdint>
#include <string>

#include <Windows.h>

namespace AsusFanControlNativeCore
{
class AsusWinIoBindings
{
public:
    AsusWinIoBindings() = default;
    ~AsusWinIoBindings();

    AsusWinIoBindings(AsusWinIoBindings const&) = delete;
    AsusWinIoBindings& operator=(AsusWinIoBindings const&) = delete;

    bool Load(std::wstring& errorMessage);
    void Unload();

    bool IsLoaded() const noexcept;

    void InitializeWinIo();
    void ShutdownWinIo();

    int FanCount() const;
    void SetFanIndex(std::uint8_t index) const;
    int FanRpm() const;
    void SetFanTestMode(bool enabled) const;
    void SetFanPwmDuty(short duty) const;
    std::uint64_t ReadCpuTemperature() const;

private:
    using InitializeWinIoFn = void (__stdcall*)();
    using ShutdownWinIoFn = void (__stdcall*)();
    using FanCountsFn = int (__stdcall*)();
    using SetFanIndexFn = void (__stdcall*)(std::uint8_t);
    using FanRpmFn = int (__stdcall*)();
    using SetFanTestModeFn = void (__stdcall*)(char);
    using SetFanPwmDutyFn = void (__stdcall*)(short);
    using ReadCpuTemperatureFn = std::uint64_t (__stdcall*)();

    void EnsureLoaded() const;
    bool ResolveFunctions(std::wstring& errorMessage);
    static std::wstring FormatWin32Error(DWORD errorCode);

    HMODULE module_{ nullptr };
    bool initialized_{ false };

    InitializeWinIoFn initializeWinIo_{ nullptr };
    ShutdownWinIoFn shutdownWinIo_{ nullptr };
    FanCountsFn fanCounts_{ nullptr };
    SetFanIndexFn setFanIndex_{ nullptr };
    FanRpmFn fanRpm_{ nullptr };
    SetFanTestModeFn setFanTestMode_{ nullptr };
    SetFanPwmDutyFn setFanPwmDuty_{ nullptr };
    ReadCpuTemperatureFn readCpuTemperature_{ nullptr };
};
}

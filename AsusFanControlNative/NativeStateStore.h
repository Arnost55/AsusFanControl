#pragma once

#include "pch.h"

namespace AsusFanControlNative
{
    struct NativeAppState
    {
        int WindowX{ 0 };
        int WindowY{ 0 };
        int WindowWidth{ 1180 };
        int WindowHeight{ 820 };
        bool HasWindowBounds{ false };

        int RequestedSpeed{ 90 };
        bool LiveApply{ false };
        bool TurnOffControlOnExit{ true };
        bool SafeClamp{ true };
        bool MinimizeToTrayOnClose{ false };
        bool AutoRefreshStats{ false };
    };

    class NativeStateStore
    {
    public:
        NativeAppState Load() const;
        bool Save(NativeAppState const& state, DWORD* errorCode = nullptr) const;
        std::wstring FilePath() const;

    private:
        std::wstring DirectoryPath() const;
        static std::wstring GetLocalAppDataPath();
        static void EnsureDirectoryExists(std::wstring const& directoryPath);
    };
}

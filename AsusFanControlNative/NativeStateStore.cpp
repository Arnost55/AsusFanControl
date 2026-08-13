#include "pch.h"
#include "NativeStateStore.h"

namespace
{
    int ClampInt(int value, int minValue, int maxValue)
    {
        if (value < minValue)
        {
            return minValue;
        }

        if (value > maxValue)
        {
            return maxValue;
        }

        return value;
    }

    bool CanOpenForRead(std::wstring const& filePath)
    {
        const HANDLE file = CreateFileW(
            filePath.c_str(),
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            nullptr,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            nullptr);
        if (file == INVALID_HANDLE_VALUE)
        {
            return false;
        }

        CloseHandle(file);
        return true;
    }

    bool TryGetLastWriteTime(std::wstring const& filePath, FILETIME& lastWriteTime)
    {
        WIN32_FILE_ATTRIBUTE_DATA data{};
        if (!GetFileAttributesExW(filePath.c_str(), GetFileExInfoStandard, &data))
        {
            return false;
        }

        lastWriteTime = data.ftLastWriteTime;
        return true;
    }

    std::wstring ChooseReadableStateFile(std::wstring const& primaryPath, std::wstring const& backupPath)
    {
        const bool primaryReadable = CanOpenForRead(primaryPath);
        const bool backupReadable = CanOpenForRead(backupPath);

        if (primaryReadable && backupReadable)
        {
            FILETIME primaryWriteTime{};
            FILETIME backupWriteTime{};
            if (TryGetLastWriteTime(primaryPath, primaryWriteTime) && TryGetLastWriteTime(backupPath, backupWriteTime))
            {
                if (CompareFileTime(&backupWriteTime, &primaryWriteTime) > 0)
                {
                    return backupPath;
                }
            }
        }

        if (primaryReadable)
        {
            return primaryPath;
        }

        if (backupReadable)
        {
            return backupPath;
        }

        return primaryPath;
    }

    AsusFanControlNative::NativeAppState LoadStateFile(std::wstring const& filePath)
    {
        AsusFanControlNative::NativeAppState state;

        state.WindowX = GetPrivateProfileIntW(L"Window", L"X", state.WindowX, filePath.c_str());
        state.WindowY = GetPrivateProfileIntW(L"Window", L"Y", state.WindowY, filePath.c_str());
        state.WindowWidth = ClampInt(GetPrivateProfileIntW(L"Window", L"Width", state.WindowWidth, filePath.c_str()), 480, 4000);
        state.WindowHeight = ClampInt(GetPrivateProfileIntW(L"Window", L"Height", state.WindowHeight, filePath.c_str()), 320, 3000);
        state.HasWindowBounds = GetPrivateProfileIntW(L"Window", L"HasBounds", state.HasWindowBounds ? 1 : 0, filePath.c_str()) != 0;

        state.RequestedSpeed = ClampInt(GetPrivateProfileIntW(L"Settings", L"RequestedSpeed", state.RequestedSpeed, filePath.c_str()), 0, 100);
        state.LiveApply = GetPrivateProfileIntW(L"Settings", L"LiveApply", state.LiveApply ? 1 : 0, filePath.c_str()) != 0;
        state.TurnOffControlOnExit = GetPrivateProfileIntW(L"Settings", L"TurnOffControlOnExit", state.TurnOffControlOnExit ? 1 : 0, filePath.c_str()) != 0;
        state.SafeClamp = GetPrivateProfileIntW(L"Settings", L"SafeClamp", state.SafeClamp ? 1 : 0, filePath.c_str()) != 0;
        state.MinimizeToTrayOnClose = GetPrivateProfileIntW(L"Settings", L"MinimizeToTrayOnClose", state.MinimizeToTrayOnClose ? 1 : 0, filePath.c_str()) != 0;
        state.AutoRefreshStats = GetPrivateProfileIntW(L"Settings", L"AutoRefreshStats", state.AutoRefreshStats ? 1 : 0, filePath.c_str()) != 0;

        return state;
    }

    bool WriteStateFile(std::wstring const& filePath, std::string const& contents, DWORD* errorCode)
    {
        const HANDLE file = CreateFileW(
            filePath.c_str(),
            GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            nullptr,
            CREATE_ALWAYS,
            FILE_ATTRIBUTE_NORMAL,
            nullptr);
        if (file == INVALID_HANDLE_VALUE)
        {
            const DWORD openError = GetLastError();
            if (openError == ERROR_ACCESS_DENIED)
            {
                SetFileAttributesW(filePath.c_str(), FILE_ATTRIBUTE_NORMAL);
                const HANDLE retryFile = CreateFileW(
                    filePath.c_str(),
                    GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                    nullptr,
                    CREATE_ALWAYS,
                    FILE_ATTRIBUTE_NORMAL,
                    nullptr);
                if (retryFile != INVALID_HANDLE_VALUE)
                {
                    const DWORD bytesToWrite = static_cast<DWORD>(contents.size());
                    DWORD bytesWritten = 0;
                    const BOOL retryWriteResult = WriteFile(retryFile, contents.data(), bytesToWrite, &bytesWritten, nullptr);
                    CloseHandle(retryFile);
                    if (retryWriteResult && bytesWritten == bytesToWrite)
                    {
                        if (errorCode != nullptr)
                        {
                            *errorCode = ERROR_SUCCESS;
                        }

                        return true;
                    }

                    if (errorCode != nullptr)
                    {
                        *errorCode = retryWriteResult ? ERROR_WRITE_FAULT : GetLastError();
                    }

                    return false;
                }
            }

            if (errorCode != nullptr)
            {
                *errorCode = openError;
            }

            return false;
        }

        const DWORD bytesToWrite = static_cast<DWORD>(contents.size());
        DWORD bytesWritten = 0;
        const BOOL writeResult = WriteFile(file, contents.data(), bytesToWrite, &bytesWritten, nullptr);
        CloseHandle(file);
        if (!writeResult || bytesWritten != bytesToWrite)
        {
            if (errorCode != nullptr)
            {
                *errorCode = writeResult ? ERROR_WRITE_FAULT : GetLastError();
            }

            return false;
        }

        if (errorCode != nullptr)
        {
            *errorCode = ERROR_SUCCESS;
        }

        return true;
    }
}

namespace AsusFanControlNative
{
    std::wstring NativeStateStore::GetLocalAppDataPath()
    {
        wchar_t buffer[MAX_PATH] = {};
        const DWORD bufferCount = static_cast<DWORD>(sizeof(buffer) / sizeof(buffer[0]));
        const DWORD length = GetEnvironmentVariableW(L"LOCALAPPDATA", buffer, bufferCount);
        if (length == 0 || length >= bufferCount)
        {
            return L".";
        }

        return std::wstring(buffer, length);
    }

    std::wstring NativeStateStore::DirectoryPath() const
    {
        return GetLocalAppDataPath() + L"\\AsusFanControl";
    }

    std::wstring NativeStateStore::FilePath() const
    {
        return DirectoryPath() + L"\\state.ini";
    }

    void NativeStateStore::EnsureDirectoryExists(std::wstring const& directoryPath)
    {
        if (directoryPath.empty())
        {
            return;
        }

        CreateDirectoryW(directoryPath.c_str(), nullptr);
    }

    NativeAppState NativeStateStore::Load() const
    {
        const std::wstring primaryPath = FilePath();
        const std::wstring backupPath = DirectoryPath() + L"\\state.backup.ini";
        const std::wstring chosenPath = ChooseReadableStateFile(primaryPath, backupPath);
        return LoadStateFile(chosenPath);
    }

    bool NativeStateStore::Save(NativeAppState const& state, DWORD* errorCode) const
    {
        const std::wstring directoryPath = DirectoryPath();
        EnsureDirectoryExists(directoryPath);

        const std::wstring primaryPath = FilePath();
        const std::wstring backupPath = directoryPath + L"\\state.backup.ini";
        std::string contents;
        contents += "[Window]\r\n";
        contents += "X=" + std::to_string(state.WindowX) + "\r\n";
        contents += "Y=" + std::to_string(state.WindowY) + "\r\n";
        contents += "Width=" + std::to_string(state.WindowWidth) + "\r\n";
        contents += "Height=" + std::to_string(state.WindowHeight) + "\r\n";
        contents += "HasBounds=" + std::to_string(state.HasWindowBounds ? 1 : 0) + "\r\n";
        contents += "\r\n";
        contents += "[Settings]\r\n";
        contents += "RequestedSpeed=" + std::to_string(ClampInt(state.RequestedSpeed, 0, 100)) + "\r\n";
        contents += "LiveApply=" + std::to_string(state.LiveApply ? 1 : 0) + "\r\n";
        contents += "TurnOffControlOnExit=" + std::to_string(state.TurnOffControlOnExit ? 1 : 0) + "\r\n";
        contents += "SafeClamp=" + std::to_string(state.SafeClamp ? 1 : 0) + "\r\n";
        contents += "MinimizeToTrayOnClose=" + std::to_string(state.MinimizeToTrayOnClose ? 1 : 0) + "\r\n";
        contents += "AutoRefreshStats=" + std::to_string(state.AutoRefreshStats ? 1 : 0) + "\r\n";

        DWORD primaryError = ERROR_SUCCESS;
        if (WriteStateFile(primaryPath, contents, &primaryError))
        {
            WriteStateFile(backupPath, contents, nullptr);
            if (errorCode != nullptr)
            {
                *errorCode = ERROR_SUCCESS;
            }

            return true;
        }

        DWORD backupError = ERROR_SUCCESS;
        if (WriteStateFile(backupPath, contents, &backupError))
        {
            if (errorCode != nullptr)
            {
                *errorCode = ERROR_SUCCESS;
            }

            return true;
        }

        if (errorCode != nullptr)
        {
            *errorCode = primaryError;
        }

        return false;
    }
}

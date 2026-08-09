#include "pch.h"
#include "App.xaml.h"

#include <cstdio>

namespace
{
    constexpr wchar_t SingleInstanceMutexName[] = L"Local\\AsusFanControlNative.SingleInstance";
    constexpr wchar_t WindowTitlePrefix[] = L"Asus Fan Control";

    void TraceStartupMessage(char const* message)
    {
        char tempPath[MAX_PATH] = {};
        const DWORD tempPathLength = GetTempPathA(MAX_PATH, tempPath);
        if (tempPathLength == 0 || tempPathLength >= MAX_PATH)
        {
            return;
        }

        const std::string filePath = std::string(tempPath) + "AsusFanControlNative.trace.txt";
        FILE* file = nullptr;
        if (fopen_s(&file, filePath.c_str(), "a") != 0 || file == nullptr)
        {
            return;
        }

        SYSTEMTIME now{};
        GetLocalTime(&now);
        fprintf(
            file,
            "%04u-%02u-%02u %02u:%02u:%02u.%03u %s\n",
            static_cast<unsigned>(now.wYear),
            static_cast<unsigned>(now.wMonth),
            static_cast<unsigned>(now.wDay),
            static_cast<unsigned>(now.wHour),
            static_cast<unsigned>(now.wMinute),
            static_cast<unsigned>(now.wSecond),
            static_cast<unsigned>(now.wMilliseconds),
            message);
        fclose(file);
    }

    std::wstring GetExecutablePath()
    {
        wchar_t executablePath[MAX_PATH] = {};
        const DWORD bufferCount = static_cast<DWORD>(sizeof(executablePath) / sizeof(executablePath[0]));
        if (GetModuleFileNameW(nullptr, executablePath, bufferCount) == 0)
        {
            return {};
        }

        return executablePath;
    }

    std::wstring GetExecutableDirectory()
    {
        std::wstring executablePath = GetExecutablePath();
        if (executablePath.empty())
        {
            return {};
        }

        const std::size_t slash = executablePath.find_last_of(L"\\/");
        if (slash == std::wstring::npos)
        {
            return {};
        }

        return executablePath.substr(0, slash + 1);
    }

    std::wstring GetDocumentsPsExecPath()
    {
        wchar_t documentsPath[MAX_PATH] = {};
        if (SHGetFolderPathW(nullptr, CSIDL_PERSONAL, nullptr, SHGFP_TYPE_CURRENT, documentsPath) != S_OK)
        {
            return {};
        }

        return std::wstring(documentsPath) + L"\\AsusFanControl\\PsExec.exe";
    }

    std::wstring FindPsExecPath()
    {
        const std::wstring executableDirectory = GetExecutableDirectory();
        if (!executableDirectory.empty())
        {
            const std::wstring localCandidate = executableDirectory + L"PsExec.exe";
            if (GetFileAttributesW(localCandidate.c_str()) != INVALID_FILE_ATTRIBUTES)
            {
                return localCandidate;
            }
        }

        const std::wstring documentsCandidate = GetDocumentsPsExecPath();
        if (!documentsCandidate.empty() && GetFileAttributesW(documentsCandidate.c_str()) != INVALID_FILE_ATTRIBUTES)
        {
            return documentsCandidate;
        }

        return {};
    }

    bool IsRunningElevated()
    {
        BOOL isMember = FALSE;
        SID_IDENTIFIER_AUTHORITY authority = SECURITY_NT_AUTHORITY;
        PSID adminGroup = nullptr;

        if (AllocateAndInitializeSid(
                &authority,
                2,
                SECURITY_BUILTIN_DOMAIN_RID,
                DOMAIN_ALIAS_RID_ADMINS,
                0,
                0,
                0,
                0,
                0,
                0,
                &adminGroup))
        {
            CheckTokenMembership(nullptr, adminGroup, &isMember);
            FreeSid(adminGroup);
        }

        return isMember == TRUE;
    }

    bool IsRunningAsSystem()
    {
        HANDLE token = nullptr;
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &token))
        {
            return false;
        }

        auto closeToken = [token]()
        {
            CloseHandle(token);
        };

        DWORD tokenSize = 0;
        GetTokenInformation(token, TokenUser, nullptr, 0, &tokenSize);
        if (tokenSize == 0 && GetLastError() != ERROR_INSUFFICIENT_BUFFER)
        {
            closeToken();
            return false;
        }

        std::vector<std::uint8_t> tokenBuffer(tokenSize);
        if (!GetTokenInformation(token, TokenUser, tokenBuffer.data(), tokenSize, &tokenSize))
        {
            closeToken();
            return false;
        }

        closeToken();

        const auto* tokenUser = reinterpret_cast<TOKEN_USER const*>(tokenBuffer.data());
        if (tokenUser == nullptr || tokenUser->User.Sid == nullptr)
        {
            return false;
        }

        return IsWellKnownSid(tokenUser->User.Sid, WinLocalSystemSid) == TRUE;
    }

    bool RelaunchAsAdministrator()
    {
        const std::wstring executablePath = GetExecutablePath();
        if (executablePath.empty())
        {
            return false;
        }

        TraceStartupMessage("wWinMain before ShellExecuteW runas");
        const HINSTANCE result = ShellExecuteW(nullptr, L"runas", executablePath.c_str(), nullptr, nullptr, SW_SHOWNORMAL);
        TraceStartupMessage("wWinMain after ShellExecuteW runas");
        const std::string resultMessage = "wWinMain ShellExecuteW result=" + std::to_string(reinterpret_cast<INT_PTR>(result));
        TraceStartupMessage(resultMessage.c_str());
        return reinterpret_cast<INT_PTR>(result) > 32;
    }

    bool RelaunchAsSystem()
    {
        if (IsRunningAsSystem())
        {
            return false;
        }

        const std::wstring psexecPath = FindPsExecPath();
        if (psexecPath.empty())
        {
            TraceStartupMessage("wWinMain PsExec not found");
            return false;
        }

        const std::wstring executablePath = GetExecutablePath();
        if (executablePath.empty())
        {
            return false;
        }

        const std::wstring arguments = L"-accepteula -i -s -d \"" + executablePath + L"\"";
        TraceStartupMessage("wWinMain before PsExec SYSTEM launch");
        const HINSTANCE result = ShellExecuteW(nullptr, L"open", psexecPath.c_str(), arguments.c_str(), nullptr, SW_SHOWNORMAL);
        TraceStartupMessage("wWinMain after PsExec SYSTEM launch");
        const std::string resultMessage = "wWinMain PsExec result=" + std::to_string(reinterpret_cast<INT_PTR>(result));
        TraceStartupMessage(resultMessage.c_str());
        return reinterpret_cast<INT_PTR>(result) > 32;
    }

    struct WindowSearchContext
    {
        HWND foundWindow{ nullptr };
    };

    BOOL CALLBACK FindWindowByTitlePrefixCallback(HWND hwnd, LPARAM lParam)
    {
        auto* context = reinterpret_cast<WindowSearchContext*>(lParam);
        if (context == nullptr || context->foundWindow != nullptr)
        {
            return TRUE;
        }

        wchar_t titleBuffer[256] = {};
        const int titleLength = GetWindowTextW(hwnd, titleBuffer, _countof(titleBuffer));
        if (titleLength <= 0)
        {
            return TRUE;
        }

        std::wstring title(titleBuffer, static_cast<std::size_t>(titleLength));
        if (title.rfind(WindowTitlePrefix, 0) == 0)
        {
            context->foundWindow = hwnd;
            return FALSE;
        }

        return TRUE;
    }

    HWND FindExistingWindow()
    {
        WindowSearchContext context{};
        EnumWindows(FindWindowByTitlePrefixCallback, reinterpret_cast<LPARAM>(&context));
        return context.foundWindow;
    }

    void RestoreExistingWindow(HWND hwnd)
    {
        if (hwnd == nullptr)
        {
            return;
        }

        ShowWindow(hwnd, IsIconic(hwnd) ? SW_RESTORE : SW_SHOW);
        BringWindowToTop(hwnd);
        SetForegroundWindow(hwnd);
    }

}

int APIENTRY wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int)
{
    try
    {
        TraceStartupMessage("wWinMain enter");

        const bool isElevated = IsRunningElevated();
        const bool isSystem = IsRunningAsSystem();
        if (!isElevated)
        {
            TraceStartupMessage("wWinMain not elevated");
            if (!RelaunchAsAdministrator())
            {
                TraceStartupMessage("wWinMain elevation unavailable, continuing without elevation");
            }
            else
            {
                TraceStartupMessage("wWinMain relaunch requested");
                return 0;
            }
        }
        else if (!isSystem)
        {
            TraceStartupMessage("wWinMain elevated");
            if (RelaunchAsSystem())
            {
                TraceStartupMessage("wWinMain SYSTEM relaunch requested");
                return 0;
            }
        }

        if (HWND existingWindow = FindExistingWindow(); existingWindow != nullptr)
        {
            TraceStartupMessage("wWinMain existing window detected");
            RestoreExistingWindow(existingWindow);
            return 0;
        }

        HANDLE instanceMutex = CreateMutexW(nullptr, FALSE, SingleInstanceMutexName);
        if (instanceMutex == nullptr)
        {
            TraceStartupMessage("wWinMain mutex creation failed");
        }
        else if (GetLastError() == ERROR_ALREADY_EXISTS)
        {
            TraceStartupMessage("wWinMain existing instance detected");
            RestoreExistingWindow(FindExistingWindow());
            CloseHandle(instanceMutex);
            return 0;
        }

        TraceStartupMessage(isElevated ? (isSystem ? "wWinMain running as SYSTEM" : "wWinMain continuing as admin") : "wWinMain continuing without elevation");
        winrt::init_apartment(winrt::apartment_type::single_threaded);
        TraceStartupMessage("wWinMain apartment initialized");

        TraceStartupMessage("wWinMain before Application::Start");
        winrt::Microsoft::UI::Xaml::Application::Start([](auto&&)
        {
            TraceStartupMessage("Application::Start lambda enter");
            winrt::make<winrt::AsusFanControlNative::implementation::App>();
            TraceStartupMessage("Application::Start lambda exit");
        });
        TraceStartupMessage("wWinMain after Application::Start");
        if (instanceMutex != nullptr)
        {
            CloseHandle(instanceMutex);
        }
    }
    catch (winrt::hresult_error const& ex)
    {
        MessageBoxW(nullptr, ex.message().c_str(), L"Asus Fan Control", MB_OK | MB_ICONERROR);
        return static_cast<int>(ex.code().value);
    }
    catch (...)
    {
        MessageBoxW(nullptr, L"Unable to start Asus Fan Control.", L"Asus Fan Control", MB_OK | MB_ICONERROR);
        return 1;
    }

    return 0;
}

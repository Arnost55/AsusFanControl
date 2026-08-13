#include "pch.h"
#include "App.xaml.h"
#include "MainWindow.xaml.h"

#include <cstdio>

#if __has_include("App.g.cpp")
#include "App.g.cpp"
#endif

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Microsoft::UI::Xaml;
using namespace winrt::Microsoft::UI::Dispatching;

namespace
{
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

}

namespace winrt::AsusFanControlNative::implementation
{
    App::App()
    {
        TraceStartupMessage("App::App enter");
        InitializeComponent();
        TraceStartupMessage("App::App initialized");

#if defined _DEBUG && !defined DISABLE_XAML_GENERATED_BREAK_ON_UNHANDLED_EXCEPTION
        UnhandledException([](IInspectable const&, UnhandledExceptionEventArgs const& e)
        {
            if (IsDebuggerPresent())
            {
                [[maybe_unused]] auto const errorMessage = e.Message();
                __debugbreak();
            }
        });
#endif
    }

    void App::OnLaunched(LaunchActivatedEventArgs const&)
    {
        TraceStartupMessage("App::OnLaunched enter");
        auto dispatcherQueue = DispatcherQueue::GetForCurrentThread();
        TraceStartupMessage(dispatcherQueue ? "App::OnLaunched dispatcher queue present" : "App::OnLaunched dispatcher queue missing");
        TraceStartupMessage("App::OnLaunched before Window");
        m_window = Window();
        TraceStartupMessage("App::OnLaunched after Window");

        TraceStartupMessage("App::OnLaunched before mainPage");
        auto mainPage = winrt::make_self<MainWindow>();
        TraceStartupMessage("App::OnLaunched mainPage created");
        TraceStartupMessage("App::OnLaunched before Content");
        m_window.Content(*mainPage);
        TraceStartupMessage("App::OnLaunched after Content");
        mainPage->InitializeNativeWindow(m_window);
        TraceStartupMessage("App::OnLaunched InitializeNativeWindow done");
        m_window.Activate();
        TraceStartupMessage("App::OnLaunched Activate done");
        mainPage->InitializeStartupState();
        TraceStartupMessage("App::OnLaunched startup state initialized");
    }
}

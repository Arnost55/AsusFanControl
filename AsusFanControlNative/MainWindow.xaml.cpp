#include "pch.h"
#include "MainWindow.xaml.h"

#include <algorithm>
#include <cstdio>
#include <sstream>

#include <winrt/Microsoft.UI.Interop.h>
#include <winrt/Microsoft.UI.Windowing.h>
#include <winrt/Windows.Graphics.h>

#include "MainWindow.xaml.impl.hpp"

using namespace winrt;
using namespace winrt::Windows::Foundation;
using namespace winrt::Microsoft::UI::Xaml;
using namespace winrt::Microsoft::UI::Xaml::Controls;
using namespace winrt::Microsoft::UI::Xaml::Controls::Primitives;
using namespace winrt::Microsoft::UI::Windowing;
using namespace winrt::Microsoft::UI::Dispatching;
using namespace winrt::Windows::Graphics;

namespace
{
    constexpr int SafeClampMinimum = 40;
    constexpr int SafeClampMaximum = 99;
    constexpr int UnsafeMinimum = 0;
    constexpr int UnsafeMaximum = 100;

    void TraceWindowMessage(std::string const& message)
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
            message.c_str());
        fclose(file);
    }

    std::string WideToUtf8(std::wstring const& value)
    {
        if (value.empty())
        {
            return {};
        }

        std::string fallback;
        fallback.reserve(value.size());
        for (wchar_t ch : value)
        {
            fallback.push_back(ch <= 0x7F ? static_cast<char>(ch) : '?');
        }

        const int sourceLength = static_cast<int>(value.size());
        const int required = WideCharToMultiByte(CP_UTF8, 0, value.c_str(), sourceLength, nullptr, 0, nullptr, nullptr);
        if (required <= 0)
        {
            return fallback;
        }

        std::string result(static_cast<std::size_t>(required), '\0');
        const int written = WideCharToMultiByte(CP_UTF8, 0, value.c_str(), sourceLength, result.data(), required, nullptr, nullptr);
        if (written <= 0)
        {
            return fallback;
        }

        return result;
    }

    std::string DescribeWindowRect(HWND hwnd)
    {
        RECT rect{};
        if (hwnd == nullptr || !GetWindowRect(hwnd, &rect))
        {
            return "rect=unavailable";
        }

        std::ostringstream stream;
        stream << "rect=("
            << rect.left << ","
            << rect.top << ","
            << rect.right << ","
            << rect.bottom << ") size=("
            << (rect.right - rect.left) << "x"
            << (rect.bottom - rect.top) << ")";
        return stream.str();
    }

    AppWindow GetAppWindowForHandle(HWND hwnd)
    {
        if (hwnd == nullptr)
        {
            return nullptr;
        }

        try
        {
            const auto windowId = winrt::Microsoft::UI::GetWindowIdFromWindow(hwnd);
            return AppWindow::GetFromWindowId(windowId);
        }
        catch (...)
        {
            return nullptr;
        }
    }

    void MoveWindowToPlacement(HWND hwnd, int left, int top, int width, int height)
    {
        if (hwnd == nullptr)
        {
            return;
        }

        TraceWindowMessage(
            "MoveWindowToPlacement request x="
            + std::to_string(left)
            + " y="
            + std::to_string(top)
            + " w="
            + std::to_string(width)
            + " h="
            + std::to_string(height)
            + " before "
            + DescribeWindowRect(hwnd));

        try
        {
            if (auto appWindow = GetAppWindowForHandle(hwnd))
            {
                appWindow.MoveAndResize(RectInt32{ left, top, width, height });
                TraceWindowMessage("MoveWindowToPlacement appwindow " + DescribeWindowRect(hwnd));
                return;
            }
        }
        catch (...)
        {
        }

        SetWindowPos(
            hwnd,
            nullptr,
            left,
            top,
            width,
            height,
            SWP_NOZORDER | SWP_NOACTIVATE);

        TraceWindowMessage("MoveWindowToPlacement setwindowpos " + DescribeWindowRect(hwnd));
    }

    RECT GetWorkAreaForRect(RECT const& rect)
    {
        MONITORINFO monitorInfo{};
        monitorInfo.cbSize = sizeof(monitorInfo);

        if (const HMONITOR monitor = MonitorFromRect(&rect, MONITOR_DEFAULTTONEAREST);
            monitor != nullptr && GetMonitorInfoW(monitor, &monitorInfo))
        {
            return monitorInfo.rcWork;
        }

        RECT workArea{};
        if (SystemParametersInfoW(SPI_GETWORKAREA, 0, &workArea, 0))
        {
            return workArea;
        }

        workArea.left = 0;
        workArea.top = 0;
        workArea.right = 1920;
        workArea.bottom = 1080;
        return workArea;
    }

    void ClampWindowPlacementToWorkArea(
        RECT const& workArea,
        int& left,
        int& top,
        int& width,
        int& height)
    {
        constexpr int MinimumWidth = 960;
        constexpr int MinimumHeight = 720;

        const int workWidth = static_cast<int>(workArea.right - workArea.left);
        const int workHeight = static_cast<int>(workArea.bottom - workArea.top);

        if (width < MinimumWidth)
        {
            width = MinimumWidth;
        }

        if (height < MinimumHeight)
        {
            height = MinimumHeight;
        }

        if (workWidth > 0 && width > workWidth)
        {
            width = workWidth;
        }

        if (workHeight > 0 && height > workHeight)
        {
            height = workHeight;
        }

        if (width < 1)
        {
            width = 1;
        }

        if (height < 1)
        {
            height = 1;
        }

        const int minLeft = static_cast<int>(workArea.left);
        const int minTop = static_cast<int>(workArea.top);
        const int maxLeft = static_cast<int>(workArea.right - width);
        const int maxTop = static_cast<int>(workArea.bottom - height);

        left = maxLeft < minLeft ? minLeft : std::clamp(left, minLeft, maxLeft);
        top = maxTop < minTop ? minTop : std::clamp(top, minTop, maxTop);
    }

    std::wstring FormatTemperature(std::optional<int> const& temperature)
    {
        if (!temperature.has_value())
        {
            return L"--";
        }

        return std::to_wstring(*temperature) + L" C";
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
}

namespace winrt::AsusFanControlNative::implementation
{
    MainWindow::MainWindow()
    {
        TraceWindowMessage("MainWindow::MainWindow enter");
        auto dispatcherQueue = DispatcherQueue::GetForCurrentThread();
        TraceWindowMessage(dispatcherQueue ? "MainWindow::MainWindow dispatcher queue present" : "MainWindow::MainWindow dispatcher queue missing");
        TraceWindowMessage("MainWindow::MainWindow before InitializeComponent");
        try
        {
            m_isUpdatingUi = true;
            InitializeComponent();
            m_isUpdatingUi = false;
        }
        catch (hresult_error const& ex)
        {
            m_isUpdatingUi = false;
            TraceWindowMessage(
                "MainWindow::MainWindow InitializeComponent hresult=0x"
                + std::to_string(static_cast<unsigned long>(ex.code().value))
                + " message="
                + WideToUtf8(std::wstring(ex.message().c_str())));
            throw;
        }
        catch (...)
        {
            m_isUpdatingUi = false;
            TraceWindowMessage("MainWindow::MainWindow InitializeComponent unknown exception");
            throw;
        }
        TraceWindowMessage("MainWindow::MainWindow after InitializeComponent");

        TraceWindowMessage("MainWindow::MainWindow before Load");
        m_state = m_stateStore.Load();
        TraceWindowMessage("MainWindow::MainWindow after Load");
        m_isRunningElevated = IsRunningElevated();
        m_committedSpeed = NormalizeRequestedSpeed(m_state.RequestedSpeed);
        ApplyStateToUi();
        TraceWindowMessage("MainWindow::MainWindow after ApplyStateToUi");
        TraceWindowMessage("MainWindow::MainWindow exit");
    }

    void MainWindow::InitializeNativeWindow(Window const& window)
    {
        m_window = window;

        try
        {
            auto windowNative = m_window.as<::IWindowNative>();
            windowNative->get_WindowHandle(&m_hwnd);
        }
        catch (...)
        {
            m_hwnd = nullptr;
        }

        if (m_hwnd == nullptr)
        {
            TraceWindowMessage("InitializeNativeWindow hwnd=null");
            return;
        }

        TraceWindowMessage("InitializeNativeWindow hwnd-set " + DescribeWindowRect(m_hwnd));
        SetWindowTextW(m_hwnd, L"Asus Fan Control");

        if (m_state.HasWindowBounds)
        {
            RestoreWindowPlacement();
        }

        m_tray.Attach(
            m_hwnd,
            [this]()
            {
                return m_state.MinimizeToTrayOnClose;
            },
            [this]()
            {
                RestoreFromTray();
            },
            [this]()
            {
                RefreshDashboard();
            },
            [this]()
            {
                RequestExit();
            },
            [this]()
            {
                SaveWindowPlacement();
            });
    }

    void MainWindow::InitializeStartupState()
    {
        TraceWindowMessage("InitializeStartupState");

        if (m_startupInitialized)
        {
            return;
        }

        m_startupInitialized = true;

        if (m_hwnd != nullptr)
        {
            if (auto appWindow = GetAppWindowForHandle(m_hwnd))
            {
                m_presenter = OverlappedPresenter::Create();
                m_presenter.PreferredMinimumWidth(960);
                m_presenter.PreferredMinimumHeight(720);
                appWindow.SetPresenter(m_presenter);
            }
        }

        ApplyInitialWindowPlacement();
        RefreshDashboard();
        BeginDeferredStartup();
        TraceWindowMessage("InitializeStartupState persist state");
        PersistState();
    }

    void MainWindow::ApplyDefaultWindowPlacement()
    {
        if (m_hwnd == nullptr)
        {
            return;
        }

        RECT workArea{};
        if (!SystemParametersInfoW(SPI_GETWORKAREA, 0, &workArea, 0))
        {
            workArea.left = 0;
            workArea.top = 0;
            workArea.right = 1920;
            workArea.bottom = 1080;
        }

        const int width = std::clamp(m_state.WindowWidth, 960, 2400);
        const int height = std::clamp(m_state.WindowHeight, 720, 1800);
        int workWidth = static_cast<int>(workArea.right - workArea.left);
        int workHeight = static_cast<int>(workArea.bottom - workArea.top);
        if (workWidth < 0)
        {
            workWidth = 0;
        }

        if (workHeight < 0)
        {
            workHeight = 0;
        }

        const int left = static_cast<int>(workArea.left) + ((workWidth - width) > 0 ? (workWidth - width) / 2 : 0);
        const int top = static_cast<int>(workArea.top) + ((workHeight - height) > 0 ? (workHeight - height) / 2 : 0);

        TraceWindowMessage(
            "ApplyDefaultWindowPlacement x="
            + std::to_string(left)
            + " y="
            + std::to_string(top)
            + " w="
            + std::to_string(width)
            + " h="
            + std::to_string(height));

        m_isApplyingWindowPlacement = true;
        MoveWindowToPlacement(m_hwnd, left, top, width, height);
        m_isApplyingWindowPlacement = false;
    }

    void MainWindow::ApplyInitialWindowPlacement()
    {
        TraceWindowMessage(
            "ApplyInitialWindowPlacement hwnd="
            + std::string(m_hwnd == nullptr ? "null" : "set")
            + " hasBounds="
            + std::string(m_state.HasWindowBounds ? "true" : "false")
            + " "
            + DescribeWindowRect(m_hwnd));

        if (m_hwnd == nullptr)
        {
            return;
        }

        if (m_state.HasWindowBounds)
        {
            m_isApplyingWindowPlacement = true;
            RestoreWindowPlacement();
            m_isApplyingWindowPlacement = false;
            TraceWindowMessage("ApplyInitialWindowPlacement restored " + DescribeWindowRect(m_hwnd));
            return;
        }

        ApplyDefaultWindowPlacement();
    }

    void MainWindow::BeginDeferredStartup()
    {
        auto dispatcherQueue = DispatcherQueue::GetForCurrentThread();
        if (dispatcherQueue == nullptr)
        {
            return;
        }

        if (m_startupPlacementTimer != nullptr)
        {
            m_startupPlacementTimer.Stop();
        }

        m_startupPlacementTimer = dispatcherQueue.CreateTimer();
        m_startupPlacementTimer.Interval(std::chrono::milliseconds(1000));
        m_startupPlacementTimer.IsRepeating(false);
        m_startupPlacementTimer.Tick([this](auto const&, auto const&)
        {
            if (m_isExiting)
            {
                return;
            }

            ApplyInitialWindowPlacement();
            InitializeRefreshTimer();
            RefreshDashboard();
        });

        m_startupPlacementTimer.Start();
    }

    void MainWindow::InitializeRefreshTimer()
    {
        auto dispatcherQueue = DispatcherQueue::GetForCurrentThread();
        if (dispatcherQueue == nullptr)
        {
            return;
        }

        m_refreshTimer = dispatcherQueue.CreateTimer();
        m_refreshTimer.Interval(std::chrono::seconds(2));
        m_refreshTimer.IsRepeating(true);
        m_refreshTimer.Tick([this](auto const&, auto const&)
        {
            if (!m_isRefreshing && m_state.AutoRefreshStats)
            {
                RefreshDashboard();
            }
        });

        UpdateRefreshTimer();
    }

    void MainWindow::ApplyStateToUi()
    {
        m_isUpdatingUi = true;

        TurnOffOnExitToggle().IsOn(m_state.TurnOffControlOnExit);
        SafeClampToggle().IsOn(m_state.SafeClamp);
        LiveApplyToggle().IsOn(m_state.LiveApply);
        MinimizeToTrayToggle().IsOn(m_state.MinimizeToTrayOnClose);
        AutoRefreshToggle().IsOn(m_state.AutoRefreshStats);
        SpeedSlider().Value(static_cast<double>(NormalizeRequestedSpeed(m_state.RequestedSpeed)));

        m_isUpdatingUi = false;

        UpdateClampRange();
        UpdateRequestedSpeedLabel();
        UpdateRefreshTimer();
        UpdateActionButtons();
    }

    void MainWindow::UpdateClampRange()
    {
        const bool safeClamp = SafeClampToggle().IsOn();
        const double minimum = safeClamp ? static_cast<double>(SafeClampMinimum) : static_cast<double>(UnsafeMinimum);
        const double maximum = safeClamp ? static_cast<double>(SafeClampMaximum) : static_cast<double>(UnsafeMaximum);

        SpeedSlider().Minimum(minimum);
        SpeedSlider().Maximum(maximum);

        const int normalized = NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value()));
        if (normalized != static_cast<int>(SpeedSlider().Value()))
        {
            m_isUpdatingUi = true;
            SpeedSlider().Value(static_cast<double>(normalized));
            m_isUpdatingUi = false;
        }

        m_state.RequestedSpeed = normalized;
        UpdateRequestedSpeedLabel();
    }

    void MainWindow::UpdateRefreshTimer()
    {
        if (m_refreshTimer == nullptr)
        {
            return;
        }

        if (m_state.AutoRefreshStats)
        {
            m_refreshTimer.Start();
        }
        else
        {
            m_refreshTimer.Stop();
        }
    }

    void MainWindow::UpdateRequestedSpeedLabel()
    {
        RequestedSpeedValue().Text(hstring{ std::to_wstring(static_cast<int>(SpeedSlider().Value())) + L"%" });
    }

    void MainWindow::UpdateActionButtons()
    {
        ApplyButton().Content(box_value(hstring{ m_state.LiveApply ? L"Apply staged changes" : L"Apply to hardware" }));
        ApplyButton().IsEnabled(m_hardwareReady);
        DiscardButton().IsEnabled(static_cast<int>(SpeedSlider().Value()) != NormalizeRequestedSpeed(m_committedSpeed));
        UpdateStatusSummary();
    }

    int MainWindow::NormalizeRequestedSpeed(int speed) const
    {
        if (m_state.SafeClamp)
        {
            return std::clamp(speed, SafeClampMinimum, SafeClampMaximum);
        }

        return std::clamp(speed, UnsafeMinimum, UnsafeMaximum);
    }

    void MainWindow::PersistState()
    {
        if (m_hwnd != nullptr)
        {
            SaveWindowPlacement();
        }

        m_state.RequestedSpeed = NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value()));
        m_state.LiveApply = LiveApplyToggle().IsOn();
        m_state.TurnOffControlOnExit = TurnOffOnExitToggle().IsOn();
        m_state.SafeClamp = SafeClampToggle().IsOn();
        m_state.MinimizeToTrayOnClose = MinimizeToTrayToggle().IsOn();
        m_state.AutoRefreshStats = AutoRefreshToggle().IsOn();

        DWORD saveError = ERROR_SUCCESS;
        const bool saveSucceeded = m_stateStore.Save(m_state, &saveError);
        const std::wstring filePath = m_stateStore.FilePath();
        const DWORD attributes = GetFileAttributesW(filePath.c_str());
        TraceWindowMessage(
            "PersistState wrote "
            + WideToUtf8(filePath)
            + " save="
            + std::string(saveSucceeded ? "true" : "false")
            + " error="
            + std::to_string(saveError)
            + " exists="
            + std::string(attributes != INVALID_FILE_ATTRIBUTES ? "true" : "false"));
    }

    void MainWindow::SaveWindowPlacement()
    {
        if (m_hwnd == nullptr)
        {
            return;
        }

        WINDOWPLACEMENT placement{};
        placement.length = sizeof(placement);
        if (!GetWindowPlacement(m_hwnd, &placement))
        {
            return;
        }

        const RECT bounds = placement.rcNormalPosition;
        if (bounds.right <= bounds.left || bounds.bottom <= bounds.top)
        {
            return;
        }

        m_state.WindowX = bounds.left;
        m_state.WindowY = bounds.top;
        m_state.WindowWidth = bounds.right - bounds.left;
        m_state.WindowHeight = bounds.bottom - bounds.top;
        m_state.HasWindowBounds = true;
    }

    void MainWindow::RestoreWindowPlacement()
    {
        if (m_hwnd == nullptr || !m_state.HasWindowBounds)
        {
            return;
        }

        const RECT savedRect{
            m_state.WindowX,
            m_state.WindowY,
            m_state.WindowX + m_state.WindowWidth,
            m_state.WindowY + m_state.WindowHeight};
        const RECT workArea = GetWorkAreaForRect(savedRect);

        int left = m_state.WindowX;
        int top = m_state.WindowY;
        int width = m_state.WindowWidth;
        int height = m_state.WindowHeight;
        ClampWindowPlacementToWorkArea(workArea, left, top, width, height);

        TraceWindowMessage(
            "RestoreWindowPlacement x="
            + std::to_string(m_state.WindowX)
            + " y="
            + std::to_string(m_state.WindowY)
            + " w="
            + std::to_string(m_state.WindowWidth)
            + " h="
            + std::to_string(m_state.WindowHeight)
            + " clamped x="
            + std::to_string(left)
            + " y="
            + std::to_string(top)
            + " w="
            + std::to_string(width)
            + " h="
            + std::to_string(height));

        m_isApplyingWindowPlacement = true;
        MoveWindowToPlacement(
            m_hwnd,
            left,
            top,
            width,
            height);
        m_isApplyingWindowPlacement = false;
    }

    void MainWindow::RestoreFromTray()
    {
        if (m_hwnd == nullptr)
        {
            return;
        }

        m_tray.HideIcon();
        ShowWindow(m_hwnd, SW_RESTORE);
        m_window.Activate();
        SetForegroundWindow(m_hwnd);
    }

    void MainWindow::RequestExit()
    {
        if (m_isExiting)
        {
            return;
        }

        m_isExiting = true;
        m_tray.SetExitRequested(true);
        PersistState();

        if (m_state.TurnOffControlOnExit)
        {
            m_hardware.DisableControl();
        }

        m_tray.HideIcon();
        m_window.Close();
    }

    void MainWindow::RefreshClicked(IInspectable const&, RoutedEventArgs const&)
    {
        RefreshDashboard();
    }

    void MainWindow::ApplyClicked(IInspectable const&, RoutedEventArgs const&)
    {
        if (!m_state.LiveApply)
        {
            LiveApplyToggle().IsOn(true);
            return;
        }

        ApplyHardwareSpeed(NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value())));
        UpdateActionButtons();
    }

    void MainWindow::DiscardClicked(IInspectable const&, RoutedEventArgs const&)
    {
        const int rollbackSpeed = NormalizeRequestedSpeed(m_committedSpeed);
        if (static_cast<int>(SpeedSlider().Value()) != rollbackSpeed)
        {
            m_isUpdatingUi = true;
            SpeedSlider().Value(static_cast<double>(rollbackSpeed));
            m_isUpdatingUi = false;
        }

        m_state.RequestedSpeed = rollbackSpeed;
        UpdateRequestedSpeedLabel();
        PersistState();
        UpdateActionButtons();

        if (m_state.LiveApply)
        {
            ApplyHardwareSpeed(rollbackSpeed);
        }
    }

    void MainWindow::ExitClicked(IInspectable const&, RoutedEventArgs const&)
    {
        RequestExit();
    }

    void MainWindow::PresetButton_Click(IInspectable const& sender, RoutedEventArgs const&)
    {
        if (auto button = sender.try_as<Button>())
        {
            try
            {
                const auto presetValue = unbox_value<hstring>(button.Tag());
                SetRequestedSpeed(std::stoi(std::wstring(presetValue.c_str())), false);
            }
            catch (...)
            {
                SetRequestedSpeed(55, false);
            }
        }
    }

    void MainWindow::SpeedSlider_ValueChanged(IInspectable const&, RangeBaseValueChangedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        const int normalized = NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value()));
        if (normalized != static_cast<int>(SpeedSlider().Value()))
        {
            m_isUpdatingUi = true;
            SpeedSlider().Value(static_cast<double>(normalized));
            m_isUpdatingUi = false;
        }

        m_state.RequestedSpeed = normalized;
        UpdateRequestedSpeedLabel();
        PersistState();
        UpdateActionButtons();

        if (m_state.LiveApply)
        {
            ApplyHardwareSpeed(normalized);
        }
    }

    void MainWindow::TurnOffOnExit_Toggled(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        m_state.TurnOffControlOnExit = TurnOffOnExitToggle().IsOn();
        PersistState();
    }

    void MainWindow::SafeClamp_Toggled(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        m_state.SafeClamp = SafeClampToggle().IsOn();
        UpdateClampRange();
        PersistState();
        UpdateActionButtons();

        if (m_state.LiveApply)
        {
            ApplyHardwareSpeed(NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value())));
        }
    }

    void MainWindow::LiveApply_Toggled(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        m_state.LiveApply = LiveApplyToggle().IsOn();
        PersistState();
        UpdateActionButtons();

        if (m_state.LiveApply)
        {
            ApplyHardwareSpeed(NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value())));
        }
    }

    void MainWindow::MinimizeToTray_Toggled(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        m_state.MinimizeToTrayOnClose = MinimizeToTrayToggle().IsOn();
        PersistState();
    }

    void MainWindow::AutoRefresh_Toggled(IInspectable const&, RoutedEventArgs const&)
    {
        if (m_isUpdatingUi)
        {
            return;
        }

        m_state.AutoRefreshStats = AutoRefreshToggle().IsOn();
        UpdateRefreshTimer();
        PersistState();

        if (m_state.AutoRefreshStats)
        {
            RefreshDashboard();
        }
    }

    void MainWindow::SetRequestedSpeed(int speed, bool applyToHardware)
    {
        const int normalized = NormalizeRequestedSpeed(speed);

        m_isUpdatingUi = true;
        SpeedSlider().Value(static_cast<double>(normalized));
        m_isUpdatingUi = false;

        m_state.RequestedSpeed = normalized;
        UpdateRequestedSpeedLabel();
        PersistState();

        if (applyToHardware || m_state.LiveApply)
        {
            ApplyHardwareSpeed(normalized);
        }

        UpdateActionButtons();
    }

    void MainWindow::ApplyHardwareSpeed(int speed)
    {
        const auto result = m_hardware.ApplySpeedPercent(speed);
        if (!result.Success)
        {
            RefreshDashboard();
            ShowHardwareError(L"Unable to apply the selected speed.", result.ErrorMessage);
            return;
        }

        m_committedSpeed = NormalizeRequestedSpeed(speed);
        RefreshDashboard();
        UpdateActionButtons();
    }

    void MainWindow::RefreshDashboard()
    {
        TraceWindowMessage("RefreshDashboard enter");
        m_isRefreshing = true;
        m_hasHardwareSnapshot = true;
        GetLocalTime(&m_lastRefreshTimestamp);
        m_hasLastRefreshTimestamp = true;

        try
        {
            const auto snapshot = m_hardware.ReadSnapshot();
            m_hardwareReady = snapshot.Ready;
            std::wstring detailText = snapshot.ErrorMessage;
            if (!m_isRunningElevated)
            {
                if (!detailText.empty())
                {
                    detailText += L" ";
                }

                detailText += L"Running without administrator privileges.";
            }
            TraceWindowMessage(
                "RefreshDashboard snapshot ready="
                + std::string(snapshot.Ready ? "true" : "false")
                + " fans="
                + std::to_string(snapshot.FanCount())
                + " cpu="
                + (snapshot.CpuTemperature.has_value()
                    ? std::to_string(*snapshot.CpuTemperature)
                    : std::string("na")));
            HardwareStateBadge().Text(hstring{ snapshot.Ready ? L"Hardware ready" : L"Hardware unavailable" });
            StatusSubtitle().Text(hstring{ snapshot.Ready
                ? L"Connected to ASUS hardware. Staged changes stay local until you apply them."
                : L"ASUS hardware is unavailable. Telemetry and fan writes stay disabled until the bridge responds." });

            FanCountValue().Text(hstring{ std::to_wstring(snapshot.FanCount()) });
            FanSpeedsValue().Text(hstring{ JoinFanSpeeds(snapshot.FanSpeeds) });
            CpuTempValue().Text(hstring{ FormatTemperature(snapshot.CpuTemperature) });

            if (snapshot.Ready)
            {
                ErrorBanner().Visibility(Visibility::Collapsed);
            }
            else
            {
                ShowHardwareError(
                    L"ASUS hardware read failed",
                    detailText.empty()
                        ? L"Confirm ASUS System Analysis is installed and running."
                        : detailText);
            }
        }
        catch (...)
        {
            m_hardwareReady = false;
            ShowHardwareError(L"ASUS hardware read failed", L"An unexpected error occurred while reading telemetry.");
        }

        UpdateStatusSummary();
        m_isRefreshing = false;
    }

    void MainWindow::UpdateStatusSummary()
    {
        SessionStateBadge().Text(hstring{ m_isRunningElevated ? L"Admin session" : L"Standard session" });

        const int currentSpeed = NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value()));
        const int committedSpeed = NormalizeRequestedSpeed(m_committedSpeed);
        PendingStateBadge().Text(hstring{ currentSpeed == committedSpeed ? L"Synced" : L"Pending changes" });

        if (m_hasLastRefreshTimestamp)
        {
            LastRefreshText().Text(hstring{ std::wstring{ L"Last refreshed " } + FormatTimestamp(m_lastRefreshTimestamp) });
        }
        else
        {
            LastRefreshText().Text(L"Last refreshed after startup checks.");
        }

        UpdateWindowTitle();
    }

    void MainWindow::UpdateWindowTitle()
    {
        if (m_hwnd == nullptr)
        {
            return;
        }

        std::wstring title = L"Asus Fan Control";
        if (!m_hasHardwareSnapshot)
        {
            SetWindowTextW(m_hwnd, title.c_str());
            return;
        }

        title += m_hardwareReady ? L" - Hardware ready" : L" - Hardware unavailable";

        if (m_hardwareReady)
        {
            const int currentSpeed = NormalizeRequestedSpeed(static_cast<int>(SpeedSlider().Value()));
            const int committedSpeed = NormalizeRequestedSpeed(m_committedSpeed);
            if (currentSpeed != committedSpeed)
            {
                title += L" | Pending changes";
            }

            if (m_state.LiveApply)
            {
                title += L" | Live apply";
            }
        }

        SetWindowTextW(m_hwnd, title.c_str());
    }

    void MainWindow::ShowHardwareError(std::wstring const& title, std::wstring const& detail)
    {
        ErrorTitleText().Text(hstring{ title });
        ErrorDetailText().Text(hstring{ detail });
        ErrorBanner().Visibility(Visibility::Visible);
    }

    std::wstring MainWindow::FormatTimestamp(SYSTEMTIME const& timestamp)
    {
        wchar_t buffer[32] = {};
        if (swprintf_s(
                buffer,
                L"%02u:%02u:%02u",
                static_cast<unsigned>(timestamp.wHour),
                static_cast<unsigned>(timestamp.wMinute),
                static_cast<unsigned>(timestamp.wSecond)) < 0)
        {
            return L"--";
        }

        return buffer;
    }

    std::wstring MainWindow::JoinFanSpeeds(std::vector<int> const& fanSpeeds)
    {
        if (fanSpeeds.empty())
        {
            return L"--";
        }

        std::wstringstream stream;
        for (std::size_t index = 0; index < fanSpeeds.size(); ++index)
        {
            if (index > 0)
            {
                stream << L", ";
            }

            stream << fanSpeeds[index] << L" RPM";
        }

        return stream.str();
    }
}

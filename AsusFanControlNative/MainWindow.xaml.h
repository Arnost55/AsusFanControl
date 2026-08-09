#pragma once

#include "MainWindow.g.h"

#include "NativeStateStore.h"
#include "TrayIconController.h"
#include "..\AsusFanControlNativeCore\AsusHardwareCore.h"

namespace winrt::AsusFanControlNative::implementation
{
    struct MainWindow : MainWindowT<MainWindow>
    {
        MainWindow();
        void InitializeNativeWindow(winrt::Microsoft::UI::Xaml::Window const& window);
        void InitializeStartupState();
        void ApplyInitialWindowPlacement();

        void RefreshClicked(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void ApplyClicked(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void DiscardClicked(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void ExitClicked(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void PresetButton_Click(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void SpeedSlider_ValueChanged(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::Controls::Primitives::RangeBaseValueChangedEventArgs const&);
        void TurnOffOnExit_Toggled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void SafeClamp_Toggled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void LiveApply_Toggled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void MinimizeToTray_Toggled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);
        void AutoRefresh_Toggled(winrt::Windows::Foundation::IInspectable const&, winrt::Microsoft::UI::Xaml::RoutedEventArgs const&);

    private:
        void BeginDeferredStartup();
        void InitializeRefreshTimer();
        void ApplyStateToUi();
        void UpdateClampRange();
        void UpdateRefreshTimer();
        void UpdateRequestedSpeedLabel();
        void UpdateActionButtons();
        void RefreshDashboard();
        void ApplyDefaultWindowPlacement();
        void SetRequestedSpeed(int speed, bool applyToHardware);
        int NormalizeRequestedSpeed(int speed) const;
        void PersistState();
        void SaveWindowPlacement();
        void RestoreWindowPlacement();
        void RestoreFromTray();
        void RequestExit();
        void ApplyHardwareSpeed(int speed);
        void ShowHardwareError(std::wstring const& title, std::wstring const& detail);
        void UpdateStatusSummary();
        void UpdateWindowTitle();
        static std::wstring FormatTimestamp(SYSTEMTIME const& timestamp);
        static std::wstring JoinFanSpeeds(std::vector<int> const& fanSpeeds);

        winrt::Microsoft::UI::Xaml::Window m_window{ nullptr };
        ::AsusFanControlNative::NativeStateStore m_stateStore;
        ::AsusFanControlNative::NativeAppState m_state;
        int m_committedSpeed{ 90 };
        ::AsusFanControlNative::TrayIconController m_tray;
        winrt::Microsoft::UI::Windowing::OverlappedPresenter m_presenter{ nullptr };
        AsusFanControlNativeCore::AsusHardwareCore m_hardware;
        winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer m_refreshTimer{ nullptr };
        winrt::Microsoft::UI::Dispatching::DispatcherQueueTimer m_startupPlacementTimer{ nullptr };
        HWND m_hwnd{ nullptr };
        SYSTEMTIME m_lastRefreshTimestamp{};
        bool m_startupInitialized{ false };
        bool m_isApplyingWindowPlacement{ false };
        bool m_isRefreshing{ false };
        bool m_isUpdatingUi{ false };
        bool m_isExiting{ false };
        bool m_isRunningElevated{ false };
        bool m_hardwareReady{ false };
        bool m_hasHardwareSnapshot{ false };
        bool m_hasLastRefreshTimestamp{ false };
    };
}

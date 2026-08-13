#pragma once

#include "pch.h"

namespace AsusFanControlNative
{
    class TrayIconController
    {
    public:
        static constexpr UINT TrayCallbackMessage = WM_APP + 41;

        void Attach(HWND ownerWindow,
                    std::function<bool()> shouldMinimizeToTray,
                    std::function<void()> onShowRequested,
                    std::function<void()> onRefreshRequested,
                    std::function<void()> onExitRequested,
                    std::function<void()> onSaveBoundsRequested);

        void Detach();
        void ShowIcon();
        void HideIcon();
        void SetExitRequested(bool value);

    private:
        static constexpr UINT_PTR SubclassId = 0x54414E59;
        static constexpr UINT CommandShow = 1001;
        static constexpr UINT CommandRefresh = 1002;
        static constexpr UINT CommandExit = 1003;

        static LRESULT CALLBACK WindowSubclassProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam, UINT_PTR subclassId, DWORD_PTR refData);

        bool HandleWindowMessage(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam);
        bool HandleTrayCallback(HWND hwnd, LPARAM trayMessage);
        void ShowContextMenu(HWND hwnd);
        void RestoreOwner(HWND hwnd);
        void HideOwner(HWND hwnd);
        void EnsureTrayIcon(HWND hwnd);
        void RemoveTrayIcon(HWND hwnd);
        bool ShouldMinimizeToTray() const;

        HWND ownerWindow_{ nullptr };
        bool iconVisible_{ false };
        bool exitRequested_{ false };
        std::function<bool()> shouldMinimizeToTray_;
        std::function<void()> onShowRequested_;
        std::function<void()> onRefreshRequested_;
        std::function<void()> onExitRequested_;
        std::function<void()> onSaveBoundsRequested_;
    };
}

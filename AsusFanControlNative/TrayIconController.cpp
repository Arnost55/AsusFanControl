#include "pch.h"
#include "TrayIconController.h"

namespace
{
    void ConfigureNotifyIconData(NOTIFYICONDATAW& data, HWND hwnd)
    {
        ZeroMemory(&data, sizeof(data));
        data.cbSize = sizeof(data);
        data.hWnd = hwnd;
        data.uID = 1;
        data.uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP;
        data.uCallbackMessage = WM_APP + 41;
        data.hIcon = LoadIconW(nullptr, IDI_APPLICATION);
        wcscpy_s(data.szTip, _countof(data.szTip), L"Asus Fan Control");
    }
}

namespace AsusFanControlNative
{
    void TrayIconController::Attach(HWND ownerWindow,
                                    std::function<bool()> shouldMinimizeToTray,
                                    std::function<void()> onShowRequested,
                                    std::function<void()> onRefreshRequested,
                                    std::function<void()> onExitRequested,
                                    std::function<void()> onSaveBoundsRequested)
    {
        ownerWindow_ = ownerWindow;
        shouldMinimizeToTray_ = std::move(shouldMinimizeToTray);
        onShowRequested_ = std::move(onShowRequested);
        onRefreshRequested_ = std::move(onRefreshRequested);
        onExitRequested_ = std::move(onExitRequested);
        onSaveBoundsRequested_ = std::move(onSaveBoundsRequested);

        SetWindowSubclass(ownerWindow_, WindowSubclassProc, SubclassId, reinterpret_cast<DWORD_PTR>(this));
    }

    void TrayIconController::Detach()
    {
        if (ownerWindow_ != nullptr)
        {
            RemoveWindowSubclass(ownerWindow_, WindowSubclassProc, SubclassId);
            RemoveTrayIcon(ownerWindow_);
        }

        ownerWindow_ = nullptr;
        iconVisible_ = false;
        exitRequested_ = false;
        shouldMinimizeToTray_ = nullptr;
        onShowRequested_ = nullptr;
        onRefreshRequested_ = nullptr;
        onExitRequested_ = nullptr;
        onSaveBoundsRequested_ = nullptr;
    }

    void TrayIconController::ShowIcon()
    {
        if (ownerWindow_ != nullptr)
        {
            EnsureTrayIcon(ownerWindow_);
        }
    }

    void TrayIconController::HideIcon()
    {
        if (ownerWindow_ != nullptr)
        {
            RemoveTrayIcon(ownerWindow_);
        }
    }

    void TrayIconController::SetExitRequested(bool value)
    {
        exitRequested_ = value;
    }

    LRESULT CALLBACK TrayIconController::WindowSubclassProc(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam, UINT_PTR subclassId, DWORD_PTR refData)
    {
        UNREFERENCED_PARAMETER(subclassId);
        auto* controller = reinterpret_cast<TrayIconController*>(refData);
        if (controller != nullptr && controller->HandleWindowMessage(hwnd, message, wParam, lParam))
        {
            return 0;
        }

        return DefSubclassProc(hwnd, message, wParam, lParam);
    }

    bool TrayIconController::HandleWindowMessage(HWND hwnd, UINT message, WPARAM wParam, LPARAM lParam)
    {
        switch (message)
        {
        case WM_CLOSE:
            if (!exitRequested_ && ShouldMinimizeToTray())
            {
                HideOwner(hwnd);
                return true;
            }
            RemoveTrayIcon(hwnd);
            return false;

        case WM_SIZE:
            if (wParam == SIZE_MINIMIZED && !exitRequested_ && ShouldMinimizeToTray())
            {
                HideOwner(hwnd);
                return true;
            }
            if (wParam != SIZE_MINIMIZED && onSaveBoundsRequested_ != nullptr)
            {
                onSaveBoundsRequested_();
            }
            return false;

        case WM_EXITSIZEMOVE:
            if (onSaveBoundsRequested_ != nullptr)
            {
                onSaveBoundsRequested_();
            }
            return false;

        case WM_COMMAND:
            switch (LOWORD(wParam))
            {
            case CommandShow:
                RestoreOwner(hwnd);
                return true;
            case CommandRefresh:
                if (onRefreshRequested_ != nullptr)
                {
                    onRefreshRequested_();
                }
                return true;
            case CommandExit:
                if (onExitRequested_ != nullptr)
                {
                    onExitRequested_();
                }
                return true;
            default:
                return false;
            }

        case TrayCallbackMessage:
            return HandleTrayCallback(hwnd, lParam);

        case WM_NCDESTROY:
            RemoveTrayIcon(hwnd);
            RemoveWindowSubclass(hwnd, WindowSubclassProc, SubclassId);
            ownerWindow_ = nullptr;
            return true;

        default:
            return false;
        }
    }

    bool TrayIconController::HandleTrayCallback(HWND hwnd, LPARAM trayMessage)
    {
        switch (trayMessage)
        {
        case WM_LBUTTONUP:
            RestoreOwner(hwnd);
            return true;
        case WM_RBUTTONUP:
            ShowContextMenu(hwnd);
            return true;
        default:
            return false;
        }
    }

    void TrayIconController::ShowContextMenu(HWND hwnd)
    {
        HMENU menu = CreatePopupMenu();
        if (menu == nullptr)
        {
            return;
        }

        AppendMenuW(menu, MF_STRING, CommandShow, L"Show dashboard");
        AppendMenuW(menu, MF_STRING, CommandRefresh, L"Refresh now");
        AppendMenuW(menu, MF_SEPARATOR, 0, nullptr);
        AppendMenuW(menu, MF_STRING, CommandExit, L"Exit");

        POINT cursor = {};
        GetCursorPos(&cursor);
        SetForegroundWindow(hwnd);
        TrackPopupMenu(menu, TPM_LEFTALIGN | TPM_RIGHTBUTTON, cursor.x, cursor.y, 0, hwnd, nullptr);
        DestroyMenu(menu);
        PostMessageW(hwnd, WM_NULL, 0, 0);
    }

    void TrayIconController::RestoreOwner(HWND hwnd)
    {
        RemoveTrayIcon(hwnd);
        ShowWindow(hwnd, IsIconic(hwnd) ? SW_RESTORE : SW_SHOW);
        SetForegroundWindow(hwnd);
        if (onShowRequested_ != nullptr)
        {
            onShowRequested_();
        }
    }

    void TrayIconController::HideOwner(HWND hwnd)
    {
        if (onSaveBoundsRequested_ != nullptr)
        {
            onSaveBoundsRequested_();
        }

        EnsureTrayIcon(hwnd);
        ShowWindow(hwnd, SW_HIDE);
    }

    void TrayIconController::EnsureTrayIcon(HWND hwnd)
    {
        if (iconVisible_)
        {
            return;
        }

        NOTIFYICONDATAW data;
        ConfigureNotifyIconData(data, hwnd);
        if (Shell_NotifyIconW(NIM_ADD, &data))
        {
            data.uVersion = NOTIFYICON_VERSION_4;
            Shell_NotifyIconW(NIM_SETVERSION, &data);
            iconVisible_ = true;
        }
    }

    void TrayIconController::RemoveTrayIcon(HWND hwnd)
    {
        if (!iconVisible_)
        {
            return;
        }

        NOTIFYICONDATAW data;
        ConfigureNotifyIconData(data, hwnd);
        Shell_NotifyIconW(NIM_DELETE, &data);
        iconVisible_ = false;
    }

    bool TrayIconController::ShouldMinimizeToTray() const
    {
        return shouldMinimizeToTray_ != nullptr && shouldMinimizeToTray_();
    }
}

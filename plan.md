# Native Windows UI Migration Plan

## Summary
- Replace the WinForms GUI with a native `WinUI 3` desktop app written in `C++/WinRT` on the Windows App SDK.
- Keep the app fully native on Windows: no browser shell, no React Native, no Tauri, and no C# UI layer.
- Preserve the current behavior set: telemetry, manual fan curve, staged/live apply, presets, safe clamping, tray behavior, and admin launch.

## Key Changes
- Build a Fluent-style dark dashboard with native XAML layout, proper DPI scaling, and responsive resizing.
- Recreate the current sections as native components: top status strip, fan-curve editor, telemetry cards, preset chips, settings panel, and error banner.
- Port ASUS hardware access into a small native core library that wraps the vendor DLL and exposes read/apply/snapshot operations to the UI.
- Replace WinForms custom drawing with native controls and templates, using XAML resources and composition effects instead of CSS.
- Use Win32 interop for elevation, tray icon, window restore, and persistence under `%LocalAppData%\AsusFanControl`.
- Keep the existing C# app only as a reference during the migration, then retire it once the native app matches feature parity.

## Delivery Phases
- Phase 1: scaffold the native app and solution wiring.
- Phase 2: move ASUS hardware access into a small native core library with clear read/apply/snapshot APIs.
- Phase 3: rebuild the dashboard in XAML and match the existing control flow, presets, telemetry, and safety checks.
- Phase 4: restore shell behavior, including elevation, tray support, window restore, and `%LocalAppData%\AsusFanControl` persistence.
- Phase 5: run the x64 Debug/Release matrix, verify launch stability, and retire the C# UI once parity is proven.

## Test Plan
- Build `x64` Debug and Release from a clean branch or worktree.
- Launch with and without elevation and confirm the app opens normally instead of vanishing after the UAC prompt.
- Verify hardware-ready and hardware-missing states, fan-speed reads, fan-speed writes, staged/apply/discard flow, preset switching, minimize-to-tray, restore, and exit.
- Check window placement, resize behavior, and DPI scaling on common Windows display settings.
- Confirm the native app still handles ASUS service or DLL failures without crashing.

## Assumptions
- This plan is being executed in a Codex worktree; verify the canonical upstream branch before creating the migration branch.
- The first milestone is feature parity and launch stability; polish refinements can follow once the native shell is stable.

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using SnapClip.Helpers;

namespace SnapClip.Services;

/// <summary>
/// Manages system-wide global hotkey registration using Win32 APIs.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly Dictionary<int, Action> _hotkeyActions = [];
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private int _nextId = 1;

    /// <summary>
    /// Fired when a hotkey registration fails due to conflict.
    /// </summary>
    public event EventHandler<string>? HotkeyConflict;

    /// <summary>
    /// Initializes the hotkey service for the given window.
    /// </summary>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwnd = helper.Handle;
        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);
    }

    /// <summary>
    /// Registers a global hotkey with the specified modifier and key.
    /// Returns the hotkey ID, or -1 if registration failed.
    /// </summary>
    public int RegisterHotkey(ModifierKeys modifiers, Key key, Action callback)
    {
        uint fsModifiers = ConvertModifiers(modifiers);
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(key);
        int id = _nextId++;

        if (!Win32Interop.RegisterHotKey(_hwnd, id, fsModifiers | Win32Interop.MOD_NOREPEAT, vk))
        {
            string hotkeyName = $"{modifiers}+{key}";
            HotkeyConflict?.Invoke(this, hotkeyName);
            return -1;
        }

        _hotkeyActions[id] = callback;
        return id;
    }

    /// <summary>
    /// Unregisters a previously registered hotkey.
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        if (_hotkeyActions.Remove(id))
        {
            Win32Interop.UnregisterHotKey(_hwnd, id);
        }
    }

    /// <summary>
    /// Unregisters all hotkeys.
    /// </summary>
    public void UnregisterAll()
    {
        foreach (int id in _hotkeyActions.Keys.ToList())
        {
            Win32Interop.UnregisterHotKey(_hwnd, id);
        }
        _hotkeyActions.Clear();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                handled = true;
                action.Invoke();
            }
        }
        return IntPtr.Zero;
    }

    private static uint ConvertModifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= Win32Interop.MOD_ALT;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= Win32Interop.MOD_CONTROL;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= Win32Interop.MOD_SHIFT;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= Win32Interop.MOD_WIN;
        return result;
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwndSource?.RemoveHook(WndProc);
    }
}

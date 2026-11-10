using System.Drawing;
using System.Runtime.InteropServices;

namespace SolatWaktu.Widget.Services;

internal static partial class NativeMethods
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;
    private const int HwndBroadcast = 0xFFFF;
    public const string WindowTitle = "Solat Waktu Widget";

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string message);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(
        IntPtr parent,
        IntPtr childAfter,
        string? className,
        string? windowName);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private static readonly IntPtr HwndTopMost = new(-1);
    private const uint GaRoot = 2;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoActivate = 0x0010;

    /// <summary>Registered message used to wake the running widget instance.</summary>
    public static readonly int ShowWidgetMessage = unchecked((int)RegisterWindowMessage("SolatWaktuWidget.ShowWidget"));

    public static void NotifyExistingInstance()
    {
        if (ShowWidgetMessage == 0)
        {
            return;
        }

        var handle = FindWindow(null, WindowTitle);
        if (handle != IntPtr.Zero)
        {
            PostMessage(handle, (uint)ShowWidgetMessage, IntPtr.Zero, IntPtr.Zero);
            return;
        }

        // Best-effort fallback in case the window title was localised by the OS.
        PostMessage(new IntPtr(HwndBroadcast), (uint)ShowWidgetMessage, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Returns the bounds of the primary Windows taskbar. Windows 11 exposes
    /// the primary bar through Shell_TrayWnd and secondary bars through
    /// Shell_SecondaryTrayWnd; the primary bar is enough for the mini strip.
    /// </summary>
    public static bool TryGetTaskbarRect(out Rectangle bounds)
    {
        bounds = Rectangle.Empty;

        var handle = FindWindow("Shell_TrayWnd", null);
        if (handle == IntPtr.Zero)
        {
            handle = FindWindow("Shell_SecondaryTrayWnd", null);
        }

        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            return false;
        }

        bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    /// <summary>
    /// Returns the bounds of the notification area (clock, tray icons and the
    /// show-desktop strip) inside the primary taskbar.
    /// </summary>
    public static bool TryGetTaskbarTrayRect(out Rectangle bounds)
    {
        bounds = Rectangle.Empty;

        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            taskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        }

        if (taskbar == IntPtr.Zero)
        {
            return false;
        }

        var tray = FindWindowEx(taskbar, IntPtr.Zero, "TrayNotifyWnd", null);
        if (tray == IntPtr.Zero || !GetWindowRect(tray, out var rect))
        {
            return false;
        }

        bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    /// <summary>
    /// Returns the bounds of the running-application button area inside the
    /// primary taskbar, used to keep the mini bar clear of app icons.
    /// </summary>
    public static bool TryGetTaskbarAppAreaRect(out Rectangle bounds)
    {
        bounds = Rectangle.Empty;

        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            taskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        }

        if (taskbar == IntPtr.Zero)
        {
            return false;
        }

        var area = FindWindowEx(taskbar, IntPtr.Zero, "MSTaskSwWClass", null);
        if (area == IntPtr.Zero)
        {
            area = FindWindowEx(taskbar, IntPtr.Zero, "ReBarWindow32", null);
        }

        if (area == IntPtr.Zero || !GetWindowRect(area, out var rect))
        {
            return false;
        }

        bounds = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    /// <summary>
    /// Returns true when the taskbar (or one of its child windows) is the
    /// topmost window at the given screen point.
    /// </summary>
    public static bool IsTaskbarAtPoint(Point point)
    {
        var hit = WindowFromPoint(new NativePoint { X = point.X, Y = point.Y });
        if (hit == IntPtr.Zero)
        {
            return false;
        }

        var root = GetAncestor(hit, GaRoot);
        if (root == IntPtr.Zero)
        {
            return false;
        }

        var taskbar = FindWindow("Shell_TrayWnd", null);
        if (taskbar == IntPtr.Zero)
        {
            taskbar = FindWindow("Shell_SecondaryTrayWnd", null);
        }

        return taskbar != IntPtr.Zero && root == taskbar;
    }

    /// <summary>
    /// Raises the window back to the top of the topmost band without moving,
    /// resizing or activating it.
    /// </summary>
    public static void BringToTopMost(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(
            handle,
            HwndTopMost,
            0,
            0,
            0,
            0,
            SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    public static void EnableDarkTitleBar(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = 1;
        var result = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref value, sizeof(int));
        if (result != 0)
        {
            DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeLegacy, ref value, sizeof(int));
        }
    }

    public static void EnableRoundedCorners(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var value = DwmwcpRound;
        DwmSetWindowAttribute(handle, DwmwaWindowCornerPreference, ref value, sizeof(int));
    }
}

using System;
using System.Runtime.InteropServices;

namespace HarbingerWatermarkOverlay
{
    internal static class Native
    {
        public const int  GWL_EXSTYLE       = -20;
        public const int  SWP_NOSIZE        = 0x0001;
        public const int  SWP_NOMOVE        = 0x0002;
        public const int  SWP_NOACTIVATE    = 0x0010;
        public const int  SWP_SHOWWINDOW    = 0x0040;

        public const long WS_EX_LAYERED     = 0x00080000L;
        public const long WS_EX_TRANSPARENT = 0x00000020L;
        public const long WS_EX_TOOLWINDOW  = 0x00000080L;
        public const long WS_EX_NOACTIVATE  = 0x08000000L;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        // Overload for 32-bit processes (GetWindowLong)
        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        public static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Overload for 32-bit processes (SetWindowLong)
        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        // Helper wrappers that use 32/64-bit appropriate functions
        public static IntPtr GetWindowLongPtrCompat(IntPtr hWnd, int nIndex) =>
            IntPtr.Size == 8 ? GetWindowLongPtr(hWnd, nIndex) : GetWindowLong32(hWnd, nIndex);

        public static IntPtr SetWindowLongPtrCompat(IntPtr hWnd, int nIndex, IntPtr dwNewLong) =>
            IntPtr.Size == 8 ? SetWindowLongPtr(hWnd, nIndex, dwNewLong) : SetWindowLong32(hWnd, nIndex, dwNewLong);
    }
}
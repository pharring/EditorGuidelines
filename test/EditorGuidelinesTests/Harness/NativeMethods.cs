// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace EditorGuidelinesTests.Harness
{
    internal static class NativeMethods
    {
        private const string c_kernel32 = "kernel32.dll";
        private const string c_user32 = "user32.dll";

        public const uint SWP_NOZORDER = 4;

        [DllImport(c_user32, SetLastError = true)]
        public static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        [DllImport(c_user32, SetLastError = true)]
        public static extern void SwitchToThisWindow(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool fUnknown);

        [DllImport(c_user32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport(c_user32, SetLastError = false)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport(c_kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();

        [DllImport(c_kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeConsole();

        [DllImport(c_kernel32, SetLastError = false)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport(c_user32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport(c_user32, CharSet = CharSet.Unicode)]
        public static extern short VkKeyScan(char ch);
    }
}

// Copyright (c) Paul Harrington.  All Rights Reserved.  Licensed under the MIT License.  See LICENSE in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace EditorGuidelinesTests.Harness
{
    internal sealed class VisualStudioServices : AbstractServices
    {
        public VisualStudioServices(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task ActivateMainWindowAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = (EnvDTE.DTE)ServiceProvider.GetService(typeof(EnvDTE.DTE));
            var activeWindow = (IntPtr)dte.ActiveWindow.HWnd;
            if (activeWindow == IntPtr.Zero)
            {
                activeWindow = (IntPtr)dte.MainWindow.HWnd;
            }

            SetForegroundWindow(activeWindow);
        }

        private void SetForegroundWindow(IntPtr window)
        {
            TestServices.ThrowIfNotOnMainThread();

            var activeWindow = NativeMethods.GetLastActivePopup(window);
            activeWindow = NativeMethods.IsWindowVisible(activeWindow) ? activeWindow : window;
            NativeMethods.SwitchToThisWindow(activeWindow, true);

            if (!NativeMethods.SetForegroundWindow(activeWindow))
            {
                if (!NativeMethods.AllocConsole())
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                try
                {
                    var consoleWindow = NativeMethods.GetConsoleWindow();
                    if (consoleWindow == IntPtr.Zero)
                    {
                        throw new InvalidOperationException("Failed to obtain the console window.");
                    }

                    if (!NativeMethods.SetWindowPos(consoleWindow, IntPtr.Zero, 0, 0, 0, 0, NativeMethods.SWP_NOZORDER))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }
                finally
                {
                    if (!NativeMethods.FreeConsole())
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                }

                if (!NativeMethods.SetForegroundWindow(activeWindow))
                {
                    throw new InvalidOperationException("Failed to set the foreground window.");
                }
            }
        }
    }
}

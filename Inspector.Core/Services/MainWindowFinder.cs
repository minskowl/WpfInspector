using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using ChristianMoser.WpfInspector.Win32;

namespace ChristianMoser.WpfInspector.Services
{
    internal class MainWindowFinder
    {
        #region Private Members

        private IntPtr _bestHandle;
        private int _processId;

        #endregion

        // Methods
        private bool EnumWindowsCallback(IntPtr handle, IntPtr extraParameter)
        {
            int num;
            NativeMethods.GetWindowThreadProcessId(handle, out num);
            if ((num == _processId))
            {
                Debug.WriteLine("HWND="+ handle);
                if (IsMainWindow(handle))
                {
               
                    _bestHandle = handle;
                    return false;
                }

            }
            return true;
        }

        public IntPtr FindMainWindow(int processId)
        {
            _bestHandle = IntPtr.Zero;
            _processId = processId;
            var callback = new NativeMethods.EnumWindowsCallBackDelegate(EnumWindowsCallback);
            NativeMethods.EnumWindows(EnumWindowsCallback, IntPtr.Zero);
            GC.KeepAlive(callback);
            return _bestHandle;
        }

        private static bool IsMainWindow(IntPtr handle)
        {
            var owner = NativeMethods.GetWindow(handle, 4);
            int num;
            NativeMethods.GetWindowThreadProcessId(owner, out num);
            return (!(owner != IntPtr.Zero) && NativeMethods.IsWindowVisible(handle));
        }
    }


}

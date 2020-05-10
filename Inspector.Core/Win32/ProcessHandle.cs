using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace ChristianMoser.WpfInspector.Win32
{
    public class ProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private ProcessHandle()
            : base(ownsHandle: true)
        {
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            return NativeMethods.CloseHandle(handle);
        }
    }

}

using System.Runtime.InteropServices;
using System.Security;

namespace HwHash
{
    internal static partial class WinApi
    {
        [SuppressUnmanagedCodeSecurity]
        [LibraryImport("winmm.dll", EntryPoint = "timeBeginPeriod", SetLastError = true)]
        public static partial uint TimeBeginPeriod(uint uMilliseconds);

        [SuppressUnmanagedCodeSecurity]
        [LibraryImport("winmm.dll", EntryPoint = "timeEndPeriod", SetLastError = true)]
        public static partial uint TimeEndPeriod(uint uMilliseconds);
    }
}

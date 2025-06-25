using HwHash;
using System.Runtime.InteropServices;

namespace HWHash.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HwHashHeader
    {
        public uint ID;
        public uint Instance;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SENSOR_STRING_LEN)]
        public string NameDefault;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SENSOR_STRING_LEN)]
        public string NameCustom;
    }
}

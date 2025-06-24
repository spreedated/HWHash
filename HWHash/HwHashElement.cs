using System.Runtime.InteropServices;

namespace HwHash
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HwHashElement
    {
        public SENSOR_READING_TYPE SENSOR_TYPE;
        public uint Index;
        public uint ID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SENSOR_STRING_LEN)]
        public string NameDefault;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.SENSOR_STRING_LEN)]
        public string NameCustom;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = Constants.READING_STRING_LEN)]
        public string Unit;
        public double Value;
        public double ValueMin;
        public double ValueMax;
        public double ValueAvg;
    }
}

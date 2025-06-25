using System.Runtime.InteropServices;

namespace HwHash.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HwInfoMem
    {
        public uint Sig;
        public uint Ver;
        public uint Rev;
        public long PollTime;
        public uint SS_OFFSET;
        public uint SS_SIZE;
        public uint SS_SensorElements;
        public uint OFFSET_Reading;
        public uint SIZE_Reading;
        public uint TOTAL_ReadingElements;
    }
}

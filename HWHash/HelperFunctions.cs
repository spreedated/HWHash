using HwHash;
using HwHash.Models;
using System.Diagnostics;
using System.Linq;

namespace HwHash
{
    internal static class HelperFunctions
    {
        public static ulong FastConcat(uint a, uint b)
        {
            return ((ulong)a << 32) | b;
        }

        public static bool IsHWInfoRunning()
        {
            return Process.GetProcesses().Any(proc => PrecompiledRegexes.HwinfoProcess().IsMatch(proc.ProcessName));
        }

        public static int ExplicitComparison(HwInfoHash a, HwInfoHash b)
        {
            return a.IndexOrder.CompareTo(b.IndexOrder);
        }

        public static int ExplicitComparisonMini(HwinfoHashMini a, HwinfoHashMini b)
        {
            return a.IndexOrder.CompareTo(b.IndexOrder);
        }

        public static string TypeToString(SENSOR_READING_TYPE t)
        {
            int index = (int)t;
            return (index >= 0 && index < Constants.SensorTypeStrings.Length) ? Constants.SensorTypeStrings[index] : "Unknown";
        }
    }
}

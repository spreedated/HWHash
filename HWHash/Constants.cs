namespace HwHash
{
    internal static class Constants
    {
        public const string SHARED_MEM_PATH = "Global\\HWiNFO_SENS_SM2";
        public const int SENSOR_STRING_LEN = 128, READING_STRING_LEN = 16;

        public readonly static string[] RelevantSensors =
        [
            "Physical Memory Load",
            "Physical Memory Used",
            "P-core 0 VID",
            "P-core 0 Clock",
            "Ring/LLC Clock",
            "Total CPU Usage",
            "CPU Package",
            "Core Max",
            "CPU Package Power",
            "Vcore",
            "+12V",
            "SPD Hub Temperature",
            "GPU Temperature",
            "GPU Memory Junction Temperature",
            "GPU 8-pin #1 Input Voltage",
            "GPU 8-pin #2 Input Voltage",
            "GPU 8-pin #3 Input Voltage",
            "GPU Power (Total)",
            "GPU Core Load",
            "GPU Memory Controller Load",
            "Current DL rate",
            "Current UP rate",
            "Total Errors"
        ];

        public static readonly string[] SensorTypeStrings =
        [
            "None",
            "Temperature",
            "Voltage",
            "Fan",
            "Current",
            "Power",
            "Frequency",
            "Usage",
            "Other"
        ];
    }
}

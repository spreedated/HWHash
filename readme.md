# HWHash
## _HWHash Collects HWiNFO's sensor information in realtime, via shared memory and writes them directly to a easily accessible Dictionary._

This fork transforms HWHash from a singleton static class into a fully object-oriented library,
making it easier to extend, test, and integrate.

Alongside the OOP refactor, several code optimizations have been applied to further reduce overhead and improve performance.

- 🦄 Object-oriented design for better maintainability and extensibility.
- 🚀 Improved performance with optimized data handling and reduced overhead.
- 😲 Tiny footprint, no memory leaks and 0.01% CPU Usage.
- 💨 Blazing fast, <1 millisecond to iterate over 300 sensors.
- ✨ It simply works.

## Features

- Unique ID for each sensor avoid name collision
- Compatible with all HWiNFO versions with Shared Memory Support
- Collects "Parent Sensor" information such as Name, ID and Instance
- Hashes both the Sensor's Original name and the User Defined name
- Exports sensor information in the same order HWiNFO UI
- Exports to a List or JSON string in both Full and Minified versions
**check the minified struct version below.*

Usage
---

It is as simple as:
```c#
HWHash.Launch();
```
---
Options
---
There are three startup options for HWHash.

| Option | Default |
| ------ | ------ |
| HighPrecision | ![](https://img.shields.io/static/v1?label=&message=false&color=ff7da8)  |
| Delay | ![](https://img.shields.io/static/v1?label=&message=1000ms&color=b0a2f9) |

---

Default Struct
---
This is the base struct, it contains all HWiNFO sensor data, such as min, max and avg values.
```c#
 public record struct HWINFO_HASH
    {
        public string ReadingType { get; set; }
        public uint SensorIndex { get; set; }
        public uint SensorID { get; set; }
        public ulong UniqueID { get; set; }
        public string NameDefault { get; set; }
        public string NameCustom { get; set; }
        public string Unit { get; set; }
        public double ValueNow { get; set; }
        public double ValueMin { get; set; }
        public double ValueMax { get; set; }
        public double ValueAvg { get; set; }
        public string ParentNameDefault { get; set; }
        public string ParentNameCustom { get; set; }
        public uint ParentID { get; set; }
        public uint ParentInstance { get; set; }
        public ulong ParentUniqueID { get; set; }
        public int IndexOrder { get; set; }
    }
```
Minified Struct
---
The minified version is more suitable for 'realtime' monitoring, since it is packed in a much smaller package.
```c#
public record struct HWINFO_HASH_MINI
    {
        public ulong UniqueID { get; set; }
        public string NameCustom { get; set; }
        public string Unit { get; set; }
        public double ValueNow { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public int IndexOrder { get; set; }
    }
```
Relevant Sensor List
---
If you prefer to avoid manually searching for sensor IDs and wish to access a curated List<HWINFO_HASH> of relevant sensors directly, use this function.
```c#
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
```

Performance
---
You can access HWHash performance metrics by invoking the following method:
```c#
GetHWHashStats();
```
 HWHashStats *struct*
```c#
public record struct HWHashStats
{
    public long CollectionTime { get; set; }
    public uint TotalCategories { get; set; }
    public uint TotalEntries { get; set; }
}
```
The most critical information we want to inspect is
```c#
...
long ProfilingTime = _Stats.CollectionTime;
...
```
On a decent modern system, even if there are over 300 sensors, profiling times should stay <1 millisecond. Which is not a concern since HWiNFO will flush new data with a minimum delay of 100ms between readings.

[![N|Solid](https://i.imgur.com/NHrArS2.png)]()

*CollectionTime returns the time in milliseconds between each full loop, in the screenshot above, there are 359 distinct sensor readings.*

We know that for Overclockers and Hardware enthusiasts, it is important to have fast, reliable and accurate readings, and a 1 millisecond overhead is well within what is considered a safe margin.

Notes on Sensor Poll Rate
---
This library relies on a third party application, which is HWiNFO, and HWiNFO relies on the exposed sensors from your hardware, such as motherboard sensors, CPU, GPU sensors, etc. 

Usually sensor access/read is deadly fast (nanoseconds) and it is never a bottleneck. There are few rare examples, for instance, on my personal system I am currently using Corsair Vengeance memory sticks, and each memory stick has a temperature sensor, out of 359 different readings on my system, the DIMMs are the only ones who take more than nanoseconds to be read, in my case, HWiNFO takes around 6MS to poll the Memory Temperature from all chips. 

Since HWiNFO fastest "poll rate" is 50MS, it is not a problem, but it is definitely something that we should keep an eye on when reading from sensors exposed by our hardware.

To-do
---

### Lacking 👀
- [ ] Smoothing/interpolation for values
- [ ] Add the option to Flush to InfluxDB
- [ ] Option to create triggers/alerts
- [ ] Save presets and sensor preferences
- [ ] Visual interface to select/deselect sensors

### Added  💖
- [x] JSON export with no third party libraries
- [x] Add Min, Max, Average
- [x] Store previous reading value
- [x] PowerShell Integration

### License
This project is licensed under [GLWTPL](./LICENSE)
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HwHash;

public static class HwHash
{
    private static MemoryMappedFile _memMap;
    private static HwInfoMem _memRegion;
    private static HWHashStats _stats = new(0, 0, 0, 0);
    private static int _indexOrder = 0;
    private static CancellationTokenSource _pollingCTS;
    private static Task _pollingTask;
    private static readonly ILogger logger;

    private readonly static Stopwatch _benchSW = new();
    private static readonly Dictionary<uint, HwHashHeader> _headers = [];

    public readonly static ConcurrentDictionary<ulong, HwInfoHash> Sensors = new();
    public readonly static ConcurrentDictionary<ulong, HwinfoHashMini> SensorsMini = new();

    public static bool HighPriority { get; set; } = false;
    public static bool HighPrecision { get; set; } = false;
    private static int _delayMs = 1000;

    private static async Task PollSensorsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Stopwatch sw = Stopwatch.StartNew();
            ReadSensors();
            sw.Stop();
            double ms = sw.ElapsedTicks / (double)Stopwatch.Frequency * 1000;
            _stats = _stats with { CollectionTime = ms, CollectionTimeTicks = sw.ElapsedTicks };
            try { await Task.Delay(_delayMs, token); } catch (TaskCanceledException) { break; }
        }
    }

    private static void ReadSensors()
    {
        _stats = _stats with { TotalEntries = _memRegion.TOTAL_ReadingElements };
        MiniBenchmark(0);
        long totalSize = _memRegion.TOTAL_ReadingElements * _memRegion.SIZE_Reading;
        try
        {
            using (var accessor = _memMap.CreateViewAccessor(_memRegion.OFFSET_Reading, totalSize, MemoryMappedFileAccess.Read))
            {
                byte[] allData = new byte[totalSize];
                accessor.ReadArray(0, allData, 0, allData.Length);
                GCHandle handle = GCHandle.Alloc(allData, GCHandleType.Pinned);
                IntPtr basePtr = handle.AddrOfPinnedObject();
                Parallel.For(0, (int)_memRegion.TOTAL_ReadingElements,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    (int i) =>
                    {
                        IntPtr ptr = IntPtr.Add(basePtr, i * (int)_memRegion.SIZE_Reading);
                        HwHashElement reading = Marshal.PtrToStructure<HwHashElement>(ptr);
                        UpdateSensorData(reading);
                    });
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error reading sensors from shared memory.");
        }
        MiniBenchmark(1);
    }

    private static void UpdateSensorData(HwHashElement r)
    {
        ulong uid = FastConcat(r.ID, r.Index);
        if (!Sensors.ContainsKey(uid))
        {
            int order = Interlocked.Increment(ref _indexOrder) - 1;
            HwinfoHashMini mini = new HwinfoHashMini(uid, r.NameCustom, r.Unit, r.Value, r.Value, order, TypeToString(r.SENSOR_TYPE));
            HwInfoHash full = new HwInfoHash(
                TypeToString(r.SENSOR_TYPE),
                r.Index, r.ID, uid,
                r.NameDefault, r.NameCustom, r.Unit,
                r.Value, r.ValueMin, r.ValueMax, r.ValueAvg, r.Value,
                _headers[r.Index].NameDefault, _headers[r.Index].NameCustom,
                _headers[r.Index].ID, _headers[r.Index].Instance,
                FastConcat(_headers[r.Index].ID, _headers[r.Index].Instance),
                order);
            Sensors.TryAdd(uid, full);
            SensorsMini.TryAdd(uid, mini);
        }
        else
        {
            Sensors.AddOrUpdate(uid,
                (ulong key) => throw new Exception("Unexpected condition."),
                (ulong key, HwInfoHash prev) => prev with
                {
                    ValuePrev = prev.ValueNow,
                    ValueNow = r.Value,
                    ValueMin = r.ValueMin,
                    ValueMax = r.ValueMax,
                    ValueAvg = r.ValueAvg
                });
            SensorsMini.AddOrUpdate(uid,
                (ulong key) => throw new Exception("Unexpected condition."),
                (ulong key, HwinfoHashMini prev) => prev with
                {
                    ValuePrev = prev.ValueNow,
                    ValueNow = r.Value
                });
        }
    }

    private static int ExplicitComparison(HwInfoHash a, HwInfoHash b)
    {
        return a.IndexOrder.CompareTo(b.IndexOrder);
    }

    private static int ExplicitComparisonMini(HwinfoHashMini a, HwinfoHashMini b)
    {
        return a.IndexOrder.CompareTo(b.IndexOrder);
    }

    private static string TypeToString(SENSOR_READING_TYPE t)
    {
        int index = (int)t;
        return (index >= 0 && index < SensorTypeStrings.Length) ? SensorTypeStrings[index] : "Unknown";
    }

    private static bool ReadMem()
    {
        try
        {
            _memMap = MemoryMappedFile.OpenExisting(Constants.SHARED_MEM_PATH, MemoryMappedFileRights.Read);
            _memRegion = new();
            using (var accessor = _memMap.CreateViewAccessor(0L, Marshal.SizeOf(typeof(HwInfoMem)), MemoryMappedFileAccess.Read))
            {
                accessor.Read(0L, out _memRegion);
            }
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to read HWiNFO shared memory.");
            return false;
        }
    }

    private static void BuildHeaders()
    {
        long totalSize = _memRegion.SS_SensorElements * _memRegion.SS_SIZE;
        try
        {
            using (var accessor = _memMap.CreateViewAccessor(_memRegion.SS_OFFSET, totalSize, MemoryMappedFileAccess.Read))
            {
                byte[] headerData = new byte[totalSize];
                accessor.ReadArray(0, headerData, 0, headerData.Length);
                GCHandle handle = GCHandle.Alloc(headerData, GCHandleType.Pinned);
                IntPtr basePtr = handle.AddrOfPinnedObject();
                for (uint i = 0; i < _memRegion.SS_SensorElements; i++)
                {
                    IntPtr ptr = IntPtr.Add(basePtr, (int)(i * _memRegion.SS_SIZE));
                    HwHashHeader header = Marshal.PtrToStructure<HwHashHeader>(ptr);
                    _headers[i] = header;
                }
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error building headers from shared memory.");
        }
        _stats = _stats with { TotalCategories = _memRegion.SS_SensorElements };
    }
    private static void MiniBenchmark(int mode)
    {
        if (mode == 0) _benchSW.Restart();
        else _stats = _stats with { CollectionTime = _benchSW.ElapsedMilliseconds };
    }

    private static ulong FastConcat(uint a, uint b)
    {
        return ((ulong)a << 32) | b;
    }

    private static bool IsHWInfoRunning()
    {
        Process[] processes = Process.GetProcesses();
        Regex regex = new(@"hwinfo(?:32|64)?", RegexOptions.IgnoreCase);

        return processes.Any(proc => regex.IsMatch(proc.ProcessName));
    }

    private static readonly string[] SensorTypeStrings = new string[]
    {
        "None",
        "Temperature",
        "Voltage",
        "Fan",
        "Current",
        "Power",
        "Frequency",
        "Usage",
        "Other"
    };

    /// <summary>Sets polling delay in milliseconds (20–60000).</summary>
    public static bool SetDelay(int ms)
    {
        return ms >= 20 && ms <= 60000 && (_delayMs = ms, true).Item2;
    }

    /// <summary>Initializes HWHash and starts polling.</summary>
    public static bool Launch()
    {
        if (!IsHWInfoRunning())
            throw new InvalidOperationException("HWiNFO process not found.");
        if (!ReadMem()) return false;
        BuildHeaders();
        if (HighPrecision) { _ = WinApi.TimeBeginPeriod(1); }
        ReadSensors();
        _pollingCTS = new();
        _pollingTask = PollSensorsAsync(_pollingCTS.Token);
        return true;
    }

    /// <summary>Stops the polling loop.</summary>
    public static void Stop()
    {
        _pollingCTS?.Cancel();
        _pollingCTS?.Dispose();

        if (HighPrecision)
        {
            _ = WinApi.TimeEndPeriod(1);
        }
    }

    /// <summary>Returns JSON-serialized sensor data. If order==true, returns sensors in display order.</summary>
    public static string GetJsonString(bool order = false)
    {
        return order ? JsonSerializer.Serialize<List<HwInfoHash>>(GetOrderedList()) :
        JsonSerializer.Serialize<ConcurrentDictionary<ulong, HwInfoHash>>(Sensors);
    }

    /// <summary>Returns JSON-serialized minified sensor data. If order==true, returns sensors in display order.</summary>
    public static string GetJsonStringMini(bool order = false)
    {
        return order ? JsonSerializer.Serialize<List<HwinfoHashMini>>(GetOrderedListMini()) : JsonSerializer.Serialize<ConcurrentDictionary<ulong, HwinfoHashMini>>(SensorsMini);
    }

    /// <summary>Returns collection statistics (includes elapsed milliseconds and raw ticks).</summary>
    public static HWHashStats GetHWHashStats()
    {
        return _stats;
    }

    /// <summary>Returns sensors ordered by display index.</summary>
    public static List<HwInfoHash> GetOrderedList()
    {
        List<HwInfoHash> list = [.. Sensors.Values];
        list.Sort(ExplicitComparison);
        return list;
    }

    /// <summary>Returns minified sensors ordered by display index.</summary>
    public static List<HwinfoHashMini> GetOrderedListMini()
    {
        List<HwinfoHashMini> list = [.. SensorsMini.Values];
        list.Sort(ExplicitComparisonMini);
        return list;
    }

    /// <summary>Returns only relevant sensors.</summary>
    public static List<HwInfoHash> GetRelevantList()
    {
        List<HwInfoHash> list = [];
        foreach (HwInfoHash sensor in Sensors.Values.Where(sensor => Constants.RelevantSensors.Contains(sensor.NameDefault)))
        {
            string clean = sensor.NameDefault.Replace(" ", "").Replace("/", "");
            list.Add(sensor with { NameCustom = clean + sensor.SensorIndex });
        }

        list.Sort(ExplicitComparison);
        return list;
    }
}

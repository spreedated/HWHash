using HwHash;
using HwHash.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace HwHash;

public class HwHash : IDisposable
{
    internal readonly ConcurrentDictionary<ulong, HwInfoHash> Sensors = new();
    internal readonly ConcurrentDictionary<ulong, HwinfoHashMini> SensorsMini = new();

    private readonly Stopwatch benchSW = new();
    private readonly Dictionary<uint, HwHashHeader> headers = [];
    private readonly ILogger logger;
    private bool disposedValue;
    private int indexOrder = 0;
    private MemoryMappedFile memMap;
    private HwInfoMem memRegion;
    private CancellationTokenSource pollingCTS;
    private Task pollingTask;
    private HWHashStats stats = new(0, 0, 0, 0);

    public bool IsRunning
    {
        get
        {
            return this.pollingCTS != null && !this.pollingCTS.IsCancellationRequested && this.pollingTask != null && !this.pollingTask.IsCompleted;
        }
    }

    public LaunchOptions LaunchOptions { get; init; }

    public event EventHandler SensorsUpdated;
    public event EventHandler Started;
    public event EventHandler Stopped;

    #region Private Methods
    private void BuildHeaders()
    {
        long totalSize = this.memRegion.SS_SensorElements * this.memRegion.SS_SIZE;

        try
        {
            using (var accessor = this.memMap.CreateViewAccessor(this.memRegion.SS_OFFSET, totalSize, MemoryMappedFileAccess.Read))
            {
                byte[] headerData = new byte[totalSize];
                accessor.ReadArray(0, headerData, 0, headerData.Length);
                GCHandle handle = GCHandle.Alloc(headerData, GCHandleType.Pinned);
                IntPtr basePtr = handle.AddrOfPinnedObject();
                for (uint i = 0; i < this.memRegion.SS_SensorElements; i++)
                {
                    IntPtr ptr = IntPtr.Add(basePtr, (int)(i * this.memRegion.SS_SIZE));
                    HwHashHeader header = Marshal.PtrToStructure<HwHashHeader>(ptr);
                    this.headers[i] = header;
                }
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error building headers from shared memory.");
        }
        this.stats = this.stats with { TotalCategories = this.memRegion.SS_SensorElements };
    }

    private void MiniBenchmark(int mode)
    {
        if (mode == 0)
        {
            this.benchSW.Restart();
        }

        else this.stats = this.stats with { CollectionTime = this.benchSW.ElapsedMilliseconds };
    }

    private async Task PollSensorsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Stopwatch sw = Stopwatch.StartNew();
            this.ReadSensors();
            sw.Stop();

            double ms = sw.ElapsedTicks / (double)Stopwatch.Frequency * 1000;
            this.stats = stats with { CollectionTime = ms, CollectionTimeTicks = sw.ElapsedTicks };

            try
            {
                await Task.Delay(this.LaunchOptions.DelayMs, token);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private bool ReadMem()
    {
        try
        {
            this.memMap = MemoryMappedFile.OpenExisting(Constants.SHARED_MEM_PATH, MemoryMappedFileRights.Read);
            this.memRegion = new();
            using (var accessor = memMap.CreateViewAccessor(0L, Marshal.SizeOf(typeof(HwInfoMem)), MemoryMappedFileAccess.Read))
            {
                accessor.Read(0L, out this.memRegion);
            }

            return true;
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Failed to read HWiNFO shared memory.");
            return false;
        }
    }

    private void ReadSensors()
    {
        stats = stats with { TotalEntries = memRegion.TOTAL_ReadingElements };
        this.MiniBenchmark(0);
        long totalSize = memRegion.TOTAL_ReadingElements * memRegion.SIZE_Reading;

        try
        {
            using (var accessor = memMap.CreateViewAccessor(memRegion.OFFSET_Reading, totalSize, MemoryMappedFileAccess.Read))
            {
                byte[] allData = new byte[totalSize];
                accessor.ReadArray(0, allData, 0, allData.Length);
                GCHandle handle = GCHandle.Alloc(allData, GCHandleType.Pinned);
                IntPtr basePtr = handle.AddrOfPinnedObject();
                Parallel.For(0, (int)memRegion.TOTAL_ReadingElements,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                    (int i) =>
                    {
                        IntPtr ptr = IntPtr.Add(basePtr, i * (int)memRegion.SIZE_Reading);
                        HwHashElement reading = Marshal.PtrToStructure<HwHashElement>(ptr);
                        this.UpdateSensorData(reading);
                    });
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            this.logger?.LogError(ex, "Error reading sensors from shared memory.");
        }

        this.MiniBenchmark(1);
        this.SensorsUpdated?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateSensorData(HwHashElement r)
    {
        ulong uid = HelperFunctions.FastConcat(r.ID, r.Index);

        if (!this.Sensors.ContainsKey(uid))
        {
            int order = Interlocked.Increment(ref this.indexOrder) - 1;
            HwinfoHashMini mini = new(uid, r.NameCustom, r.Unit, r.Value, r.Value, order, HelperFunctions.TypeToString(r.SENSOR_TYPE));
            HwInfoHash full = new(
                HelperFunctions.TypeToString(r.SENSOR_TYPE),
                r.Index, r.ID, uid,
                r.NameDefault, r.NameCustom, r.Unit,
                r.Value, r.ValueMin, r.ValueMax, r.ValueAvg, r.Value,
                headers[r.Index].NameDefault, headers[r.Index].NameCustom,
                headers[r.Index].ID, headers[r.Index].Instance,
                HelperFunctions.FastConcat(headers[r.Index].ID, headers[r.Index].Instance),
                order);
            this.Sensors.TryAdd(uid, full);
            this.SensorsMini.TryAdd(uid, mini);
            return;
        }

        this.Sensors.AddOrUpdate(uid,
                (ulong key) => throw new InvalidOperationException("Unexpected condition."),
                (ulong key, HwInfoHash prev) => prev with
                {
                    ValuePrev = prev.ValueNow,
                    ValueNow = r.Value,
                    ValueMin = r.ValueMin,
                    ValueMax = r.ValueMax,
                    ValueAvg = r.ValueAvg
                });
        this.SensorsMini.AddOrUpdate(uid,
            (ulong key) => throw new InvalidOperationException("Unexpected condition."),
            (ulong key, HwinfoHashMini prev) => prev with
            {
                ValuePrev = prev.ValueNow,
                ValueNow = r.Value
            });
    }
    #endregion

    #region Ctor
    public HwHash(LaunchOptions options = null, ILogger logger = null)
    {
        this.logger = logger;

        if (options == null)
        {
            this.LaunchOptions = new();
        }
        else
        {
            this.LaunchOptions = options;
        }
    }
    #endregion

    #region Public Control Methods
    /// <summary>
    /// Initializes HWHash and starts polling.
    /// </summary>
    /// <returns>If successful</returns>
    public bool Start()
    {
        if (this.IsRunning)
        {
            this.logger?.LogWarning("HWHash is already running.");
            return false;
        }

        if (!HelperFunctions.IsHWInfoRunning())
        {
            this.logger?.LogError("HWiNFO process not found.");
            return false;
        }

        if (!this.ReadMem())
        {
            this.logger?.LogError("Could not read shared memory.");
            return false;
        }

        this.BuildHeaders();

        if (this.LaunchOptions.HighPrecision)
        {
            _ = WinApiPInvokes.TimeBeginPeriod(1);
        }

        this.ReadSensors();

        this.pollingCTS = new();
        this.pollingTask = this.PollSensorsAsync(pollingCTS.Token);
        this.Started?.Invoke(this, EventArgs.Empty);

        return true;
    }

    /// <summary>
    /// Stops the polling loop.
    /// </summary>
    public void Stop()
    {
        if (!this.IsRunning)
        {
            this.logger?.LogWarning("HWHash is not running.");
            return;
        }

        this.pollingCTS?.Cancel();

        if (this.LaunchOptions.HighPrecision)
        {
            _ = WinApiPInvokes.TimeEndPeriod(1);
        }

        this.Stopped?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region Public Get Methods
    /// <summary>
    /// Returns collection statistics (includes elapsed milliseconds and raw ticks).
    /// </summary>
    /// <returns></returns>
    public HWHashStats GetHWHashStats()
    {
        return this.stats;
    }

    /// <summary>
    /// Returns JSON-serialized sensor data. If order==true, returns sensors in display order.
    /// </summary>
    /// <param name="order">Should be ordered?</param>
    /// <returns></returns>
    public string GetJsonString(bool order = false)
    {
        return order ? JsonSerializer.Serialize<List<HwInfoHash>>(this.GetOrderedList()) : JsonSerializer.Serialize<ConcurrentDictionary<ulong, HwInfoHash>>(Sensors);
    }

    /// <summary>
    /// Returns JSON-serialized minified sensor data. If order==true, returns sensors in display order.
    /// </summary>
    /// <param name="order">Should be ordered?</param>
    /// <returns></returns>
    public string GetJsonStringMini(bool order = false)
    {
        return order ? JsonSerializer.Serialize<List<HwinfoHashMini>>(this.GetOrderedListMini()) : JsonSerializer.Serialize<ConcurrentDictionary<ulong, HwinfoHashMini>>(SensorsMini);
    }

    /// <summary>
    /// Returns sensors ordered by display index.
    /// </summary>
    /// <returns></returns>
    public List<HwInfoHash> GetOrderedList()
    {
        List<HwInfoHash> list = [.. this.Sensors.Values];
        list.Sort(HelperFunctions.ExplicitComparison);
        return list;
    }

    /// <summary>
    /// Returns minified sensors ordered by display index.
    /// </summary>
    /// <returns></returns>
    public List<HwinfoHashMini> GetOrderedListMini()
    {
        List<HwinfoHashMini> list = [.. this.SensorsMini.Values];
        list.Sort(HelperFunctions.ExplicitComparisonMini);
        return list;
    }

    /// <summary>
    /// Returns only relevant sensors.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<HwInfoHash> GetRelevantList()
    {
        foreach (HwInfoHash sensor in this.Sensors.Values.Where(sensor => Constants.RelevantSensors.Contains(sensor.NameDefault)))
        {
            yield return sensor with
            {
                NameCustom = sensor.NameDefault.Replace(" ", "").Replace("/", "") + sensor.SensorIndex
            };
        }
    }
    #endregion

    #region Dispose
    public void Dispose()
    {
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                this.pollingCTS?.Cancel();
                this.pollingCTS?.Dispose();
                this.memMap?.Dispose();
            }

            this.Sensors.Clear();
            this.SensorsMini.Clear();
            this.headers.Clear();
            this.benchSW.Stop();

            disposedValue = true;
            this.logger?.LogInformation("HWHash disposed");
        }

        this.logger?.LogWarning("HWHash already disposed");
    }
    #endregion
}

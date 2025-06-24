namespace HwHash
{
    public record struct HwInfoHash(
        string ReadingType,
        uint SensorIndex,
        uint SensorID,
        ulong UniqueID,
        string NameDefault,
        string NameCustom,
        string Unit,
        double ValueNow,
        double ValueMin,
        double ValueMax,
        double ValueAvg,
        double ValuePrev,
        string ParentNameDefault,
        string ParentNameCustom,
        uint ParentID,
        uint ParentInstance,
        ulong ParentUniqueID,
        int IndexOrder
    );
}

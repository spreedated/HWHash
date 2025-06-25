namespace HwHash.Models
{
    public record struct HWHashStats(double CollectionTime, long CollectionTimeTicks, uint TotalCategories, uint TotalEntries)
    {
        public HWHashStats() : this(0, 0, 0, 0) { }
    }
}

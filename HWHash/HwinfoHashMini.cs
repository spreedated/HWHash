using System.Text.Json.Serialization;

namespace HwHash
{
    public record struct HwinfoHashMini(
        ulong UniqueID,
        string NameCustom,
        string Unit,
        double ValuePrev,
        double ValueNow,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Always)] int IndexOrder,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.Always)] string ReadingType
    );
}

using System.Text.RegularExpressions;

namespace HWHash
{
    internal static partial class PrecompiledRegexes
    {
        [GeneratedRegex(@"hwinfo(?:32|64)?", RegexOptions.IgnoreCase)]
        public static partial Regex HwinfoProcess();
    }
}

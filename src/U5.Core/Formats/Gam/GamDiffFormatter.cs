using U5.Core.Utilities;

namespace U5.Core.Formats.Gam
{
    public static class GamDiffFormatter
    {
        public static string Format(GamDiffResult result)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine($"Left Length: {result.LeftLength}");
            writer.WriteLine($"Right Length: {result.RightLength}");
            writer.WriteLine($"Changed Ranges: {result.ChangedRanges.Count}");

            foreach (GamDiffRange range in result.ChangedRanges)
            {
                int endOffset = range.StartOffset + range.Length - 1;
                writer.WriteLine($"  Offset 0x{range.StartOffset:X4}-0x{endOffset:X4} (len={range.Length})");
                writer.WriteLine($"    Left : {HexFormatting.ToHex(range.LeftBytes)}");
                writer.WriteLine($"    Right: {HexFormatting.ToHex(range.RightBytes)}");
            }

            return writer.ToString();
        }
    }
}

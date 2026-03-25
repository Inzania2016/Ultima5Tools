using U5.Core.Utilities;

namespace U5.Core.Formats.Npc
{
    public static class NpcDumpFormatter
    {
        public static string Format(NpcFile npcFile)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine($"Record Count: {npcFile.Records.Count}");
            writer.WriteLine($"Expected Layout (256x18): {npcFile.IsExpectedLength}");

            foreach (NpcRecord record in npcFile.Records)
            {
                writer.WriteLine($"  [{record.Index:D3}] {HexFormatting.ToHex(record.RawBytes)}");
            }

            return writer.ToString();
        }
    }
}

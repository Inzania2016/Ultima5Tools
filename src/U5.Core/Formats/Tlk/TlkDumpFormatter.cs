namespace U5.Core.Formats.Tlk
{
    public static class TlkDumpFormatter
    {
        public static string Format(TlkFile tlkFile)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine($"NPC Count: {tlkFile.NpcCount}");
            writer.WriteLine("Entries:");

            for (int i = 0; i < tlkFile.Entries.Count; i++)
            {
                TlkEntry entry = tlkFile.Entries[i];
                string inferred = entry.InferredBlockSize.HasValue ? entry.InferredBlockSize.Value.ToString() : "unknown";
                writer.WriteLine($"  [{i:D3}] NpcId={entry.NpcId:D5} Offset=0x{entry.BlockOffset:X4} InferredSize={inferred}");
            }

            return writer.ToString();
        }
    }
}

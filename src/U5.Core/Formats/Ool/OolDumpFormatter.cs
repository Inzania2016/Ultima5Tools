using System.Text;

namespace U5.Core.Formats.Ool
{
    public static class OolDumpFormatter
    {
        public static string Format(OolFile file)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Source: {file.SourcePath}");
            sb.AppendLine($"Base name: {file.BaseName}");
            sb.AppendLine($"Kind: {file.Kind}");
            sb.AppendLine($"File size: 0x{file.FileSize:X} ({file.FileSize} bytes)");
            sb.AppendLine($"Segments: {file.Segments.Count}");
            sb.AppendLine();

            foreach (OolSegment segment in file.Segments)
            {
                IReadOnlyList<OolRecord> activeRecords = segment.Records.Where(record => !record.IsEmpty).ToArray();
                sb.AppendLine($"[{segment.SegmentIndex}] {segment.Name}");
                sb.AppendLine($"  Records: {segment.Records.Count}");
                sb.AppendLine($"  Active:  {activeRecords.Count}");
                if (activeRecords.Count == 0)
                {
                    sb.AppendLine("  (all records are zero)");
                    sb.AppendLine();
                    continue;
                }

                foreach (OolRecord record in activeRecords)
                {
                    sb.AppendLine(
                        $"  Slot {record.SlotIndex:D2} @ ({record.PositionX},{record.PositionY})  raw=[{record.GetRawTailHex()}]  words=[0x{record.RawWord23:X4} 0x{record.RawWord45:X4} 0x{record.RawWord67:X4}]");
                }

                sb.AppendLine();
            }

            if (file.Warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (string warning in file.Warnings)
                {
                    sb.AppendLine($"  {warning}");
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}

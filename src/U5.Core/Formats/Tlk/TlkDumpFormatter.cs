namespace U5.Core.Formats.Tlk
{
    public static class TlkDumpFormatter
    {
        public static string Format(TlkAnalysisReport report, TlkDataOvlDictionary? dictionary = null)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("TLK Analysis");
            writer.WriteLine($"NPC Count: {report.NpcCount}");
            writer.WriteLine($"File Size: {report.FileSize} bytes");
            writer.WriteLine($"Expected Header Size: 0x{report.ExpectedHeaderSize:X4} ({report.ExpectedHeaderSize})");
            writer.WriteLine($"Offsets Strictly Increasing: {report.OffsetsStrictlyIncreasing}");
            writer.WriteLine($"First Block Matches Header Size: {report.FirstBlockMatchesExpectedHeaderSize}");
            writer.WriteLine($"Dictionary Loaded: {report.DictionaryLoaded}");

            if (!string.IsNullOrWhiteSpace(report.DataOvlPath))
            {
                writer.WriteLine($"DATA.OVL: {report.DataOvlPath}");
            }

            writer.WriteLine();

            foreach (TlkNpcAnalysis analysis in report.NpcAnalyses)
            {
                TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis, dictionary);
                writer.Write(TlkDisassembler.RenderTextReport(disassembly));
            }

            return writer.ToString();
        }
    }
}

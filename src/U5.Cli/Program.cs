using U5.Core.Formats.Dat;
using U5.Core.Formats.Gam;
using U5.Core.Formats.Npc;
using U5.Core.Formats.Ool;
using U5.Core.Formats.Ovl;
using U5.Core.Formats.Tlk;
using U5.Core.Rendering;

namespace U5.Cli
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    Console.Error.WriteLine(UsageText.Get());
                    return 1;
                }

                string area = args[0].ToLowerInvariant();
                string action = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;

                if (area == "tlk" && action == "dump")
                {
                    return RunTlkDump(args);
                }

                if (area == "npc" && action == "dump")
                {
                    return RunNpcDump(args);
                }

                if (area == "ovl" && action == "info")
                {
                    return RunOvlInfo(args);
                }

                if (area == "ool" && action == "dump")
                {
                    return RunOolDump(args);
                }

                if (area == "gam" && action == "diff")
                {
                    return RunGamDiff(args);
                }

                if (area == "dat" && action == "info")
                {
                    return RunDatInfo(args);
                }

                if (area == "map" && action == "render")
                {
                    return RunMapRender(args);
                }

                Console.Error.WriteLine("Unknown command.");
                Console.Error.WriteLine(UsageText.Get());
                return 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Error: {exception.Message}");
                return 2;
            }
        }

        private static int RunTlkDump(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: tlk dump <path>");
                return 1;
            }

            TlkParser parser = new TlkParser();
            TlkFile file = parser.ParseFile(args[2]);

            string? dataOvlPath = DataOvlInspector.TryLocateSiblingDataOvl(args[2]);
            TlkDataOvlDictionary? dictionary = dataOvlPath is null ? null : TlkDataOvlDictionary.LoadFile(dataOvlPath);

            TlkAnalysisService analysisService = new TlkAnalysisService();
            TlkAnalysisReport report = analysisService.Analyze(file, dictionary, dataOvlPath);

            Console.WriteLine(TlkDumpFormatter.Format(report, dictionary));
            return 0;
        }

        private static int RunNpcDump(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: npc dump <path>");
                return 1;
            }

            NpcParser parser = new NpcParser();
            NpcFile file = parser.ParseFile(args[2]);

            string? dataOvlPath = DataOvlInspector.TryLocateSiblingDataOvl(args[2]);
            DataOvlInfo? dataOvlInfo = dataOvlPath is null ? null : new DataOvlInspector().InspectFile(dataOvlPath);

            Console.WriteLine(NpcDumpFormatter.Format(file, dataOvlInfo, dataOvlPath));
            return 0;
        }

        private static int RunOvlInfo(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: ovl info <path>");
                return 1;
            }

            DataOvlInspector inspector = new DataOvlInspector();
            DataOvlInfo info = inspector.InspectFile(args[2]);
            Console.WriteLine(DataOvlFormatter.Format(info));
            return 0;
        }

        private static int RunOolDump(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: ool dump <path>");
                return 1;
            }

            OolParser parser = new OolParser();
            OolFile file = parser.ParseFile(args[2]);
            Console.WriteLine(OolDumpFormatter.Format(file));
            return 0;
        }

        private static int RunGamDiff(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Usage: gam diff <leftPath> <rightPath>");
                return 1;
            }

            GamParser parser = new GamParser();
            GamFile left = parser.ParseFile(args[2]);
            GamFile right = parser.ParseFile(args[3]);

            GamDiffService diffService = new GamDiffService();
            GamDiffResult result = diffService.Diff(left, right);
            Console.WriteLine(GamDiffFormatter.Format(result));
            return 0;
        }

        private static int RunDatInfo(string[] args)
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine("Usage: dat info <path>");
                return 1;
            }

            DatInfoService service = new DatInfoService();
            DatFileInfo info = service.Inspect(args[2]);
            Console.WriteLine(DatInfoFormatter.Format(info));
            return 0;
        }

        private static int RunMapRender(string[] args)
        {
            if (args.Length != 3 && args.Length != 4)
            {
                Console.Error.WriteLine("Usage: map render <path> [outputDir]");
                return 1;
            }

            MapRenderService service = new MapRenderService();
            MapRenderResult result = service.Render(new MapRenderRequest
            {
                SourcePath = args[2],
                OutputDirectory = args.Length >= 4 ? args[3] : null
            });

            Console.WriteLine(result.Message);
            foreach (MapRenderOutputFile outputFile in result.OutputFiles)
            {
                Console.WriteLine($"  {outputFile.Kind}: {outputFile.FullPath}");
            }

            return result.IsImplemented ? 0 : 3;
        }
    }
}

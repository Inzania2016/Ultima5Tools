using U5.Core.Rendering;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class MapRenderServiceTests
    {
        [Fact]
        public void RenderTowne_WritesSvgManifestAndNpcSummary()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string samplesDirectory = Path.Combine(tempDirectory, "samples");
                string outputDirectory = Path.Combine(tempDirectory, "render-output");
                Directory.CreateDirectory(samplesDirectory);

                File.WriteAllBytes(Path.Combine(samplesDirectory, "TOWNE.DAT"), Enumerable.Repeat((byte)0x01, 16 * 1024).ToArray());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "TOWNE.NPC"), BuildNpcFile());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "TOWNE.TLK"), BuildTlkFile());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "DATA.OVL"), BuildDataOvl());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "LOOK2.DAT"), BuildLook2());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "TILES.16"), BuildTiles16());

                MapRenderService service = new MapRenderService();
                MapRenderResult result = service.Render(new MapRenderRequest
                {
                    SourcePath = Path.Combine(samplesDirectory, "TOWNE.DAT"),
                    OutputDirectory = outputDirectory
                });

                Assert.True(result.IsImplemented);
                Assert.Contains(result.OutputFiles, file => file.Kind == "svg");
                Assert.Contains(result.OutputFiles, file => file.Kind == "npc-summary");
                Assert.Contains(result.OutputFiles, file => file.Kind == "manifest");
                Assert.True(Directory.EnumerateFiles(outputDirectory, "*.svg").Any());
                Assert.True(File.Exists(Path.Combine(outputDirectory, "TOWNE_manifest.txt")));

                string firstSvg = File.ReadAllText(Directory.EnumerateFiles(outputDirectory, "TOWNE_00_*.svg").First());
                Assert.Contains("NPC overlay:", firstSvg, StringComparison.Ordinal);
                Assert.Contains("polyline", firstSvg, StringComparison.Ordinal);
                Assert.Contains("01 Malik", firstSvg, StringComparison.Ordinal);
                Assert.Contains("Malik (TLK 4)", firstSvg, StringComparison.Ordinal);
                Assert.Contains("<defs>", firstSvg, StringComparison.Ordinal);
                Assert.Contains("tile-0001", firstSvg, StringComparison.Ordinal);
                Assert.Contains("<use class=\"tile-use\"", firstSvg, StringComparison.Ordinal);

                string firstNpcSummary = File.ReadAllText(Directory.EnumerateFiles(outputDirectory, "TOWNE_00_*_npc.txt").First());
                Assert.Contains("Slot 01", firstNpcSummary, StringComparison.Ordinal);
                Assert.Contains("TLK 4: Malik", firstNpcSummary, StringComparison.Ordinal);
                Assert.Contains("Stop 0:", firstNpcSummary, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }



        [Fact]
        public void RenderUnder_WritesWorldObjectOverlayAndSummary()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string samplesDirectory = Path.Combine(tempDirectory, "samples");
                string outputDirectory = Path.Combine(tempDirectory, "render-output");
                Directory.CreateDirectory(samplesDirectory);

                File.WriteAllBytes(Path.Combine(samplesDirectory, "UNDER.DAT"), Enumerable.Repeat((byte)0x05, 256 * 256).ToArray());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "UNDER.OOL"), BuildWorldOol());
                File.WriteAllBytes(Path.Combine(samplesDirectory, "LOOK2.DAT"), BuildLook2WithMountains());

                MapRenderService service = new MapRenderService();
                MapRenderResult result = service.Render(new MapRenderRequest
                {
                    SourcePath = Path.Combine(samplesDirectory, "UNDER.DAT"),
                    OutputDirectory = outputDirectory
                });

                Assert.True(result.IsImplemented);
                Assert.Contains(result.OutputFiles, file => file.Kind == "object-summary");

                string svg = File.ReadAllText(Path.Combine(outputDirectory, "UNDER_00_underworld.svg"));
                Assert.Contains("OOL/world-object overlay:", svg, StringComparison.Ordinal);
                Assert.Contains("OOL slot 23 @ (41,41)", svg, StringComparison.Ordinal);

                string summary = File.ReadAllText(Path.Combine(outputDirectory, "UNDER_00_underworld_objects.txt"));
                Assert.Contains("Slot 23 @ (41,41)", summary, StringComparison.Ordinal);
                Assert.Contains("Raw tail: [0E F2 FF 00 00 00]", summary, StringComparison.Ordinal);
                Assert.Contains("underlying tile 0x05: mountains", summary, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }

        private static byte[] BuildTlkFile()
        {
            List<byte> block = new List<byte>();
            block.AddRange(EncodeTlkString("Malik"));
            block.AddRange(EncodeTlkString("a test npc."));
            block.AddRange(EncodeTlkString("Hello."));
            block.AddRange(EncodeTlkString("Testing."));
            block.AddRange(EncodeTlkString("Bye."));

            ushort blockOffset = 2 + 4;
            List<byte> bytes = new List<byte>();
            bytes.Add(0x01);
            bytes.Add(0x00);
            bytes.Add(0x04);
            bytes.Add(0x00);
            bytes.Add((byte)(blockOffset & 0xFF));
            bytes.Add((byte)(blockOffset >> 8));
            bytes.AddRange(block);
            return bytes.ToArray();
        }

        private static byte[] EncodeTlkString(string value)
        {
            byte[] bytes = new byte[value.Length + 1];
            for (int i = 0; i < value.Length; i++)
            {
                bytes[i] = (byte)(value[i] + 0x80);
            }

            bytes[^1] = 0x00;
            return bytes;
        }

        private static byte[] BuildNpcFile()
        {
            byte[] bytes = new byte[4608];

            // First active slot in first map block.
            int slotIndex = 1;
            int scheduleOffset = slotIndex * 16;
            bytes[scheduleOffset + 0] = 0x10;
            bytes[scheduleOffset + 1] = 0x20;
            bytes[scheduleOffset + 2] = 0x30;
            bytes[scheduleOffset + 3] = 4;
            bytes[scheduleOffset + 4] = 8;
            bytes[scheduleOffset + 5] = 12;
            bytes[scheduleOffset + 6] = 5;
            bytes[scheduleOffset + 7] = 9;
            bytes[scheduleOffset + 8] = 13;
            bytes[scheduleOffset + 9] = 0;
            bytes[scheduleOffset + 10] = 0;
            bytes[scheduleOffset + 11] = 0;
            bytes[scheduleOffset + 12] = 0x06;
            bytes[scheduleOffset + 13] = 0x0C;
            bytes[scheduleOffset + 14] = 0x12;
            bytes[scheduleOffset + 15] = 0x18;

            int typeTableOffset = 32 * 16;
            int dialogTableOffset = typeTableOffset + 32;
            bytes[typeTableOffset + slotIndex] = 7;
            bytes[dialogTableOffset + slotIndex] = 4;

            return bytes;
        }

        private static byte[] BuildDataOvl()
        {
            byte[] bytes = new byte[0x4000];
            byte[] starts = new byte[] { 0, 2, 4, 7, 8, 10, 12, 14 };
            Array.Copy(starts, 0, bytes, 0x1E2A, starts.Length);
            return bytes;
        }


        private static byte[] BuildTiles16()
        {
            byte[] bytes = new byte[0x200 * 128];
            // Tile 1: alternating blue/white stripes.
            for (int row = 0; row < 16; row++)
            {
                int rowOffset = (1 * 128) + (row * 8);
                for (int colByte = 0; colByte < 8; colByte++)
                {
                    bytes[rowOffset + colByte] = (byte)(row % 2 == 0 ? 0x19 : 0x91);
                }
            }

            return bytes;
        }


        private static byte[] BuildWorldOol()
        {
            byte[] bytes = new byte[0x100];
            int offset = 23 * 8;
            bytes[offset + 0] = 41;
            bytes[offset + 1] = 41;
            bytes[offset + 2] = 0x0E;
            bytes[offset + 3] = 0xF2;
            bytes[offset + 4] = 0xFF;
            return bytes;
        }

        private static byte[] BuildLook2WithMountains()
        {
            byte[] bytes = new byte[2048 + 64];
            BitConverter.GetBytes((ushort)0x400).CopyTo(bytes, 10);
            bytes[0x400] = (byte)'m';
            bytes[0x401] = (byte)'o';
            bytes[0x402] = (byte)'u';
            bytes[0x403] = (byte)'n';
            bytes[0x404] = (byte)'t';
            bytes[0x405] = (byte)'a';
            bytes[0x406] = (byte)'i';
            bytes[0x407] = (byte)'n';
            bytes[0x408] = (byte)'s';
            bytes[0x409] = 0x00;
            return bytes;
        }

        private static byte[] BuildLook2()
        {
            byte[] bytes = new byte[2048 + 32];
            BitConverter.GetBytes((ushort)0x400).CopyTo(bytes, 2);
            bytes[0x400] = (byte)'w';
            bytes[0x401] = (byte)'a';
            bytes[0x402] = (byte)'t';
            bytes[0x403] = (byte)'e';
            bytes[0x404] = (byte)'r';
            bytes[0x405] = 0x00;
            return bytes;
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"u5-render-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}

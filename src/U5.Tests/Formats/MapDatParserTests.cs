using U5.Core.Formats.Dat;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class MapDatParserTests
    {
        [Fact]
        public void ParseTowne_UsesDataOvlStartIndicesToGroupPlanes()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string datPath = Path.Combine(tempDirectory, "TOWNE.DAT");
                string npcPath = Path.Combine(tempDirectory, "TOWNE.NPC");
                string dataOvlPath = Path.Combine(tempDirectory, "DATA.OVL");

                File.WriteAllBytes(datPath, BuildTowneDat());
                File.WriteAllBytes(npcPath, new byte[4608]);
                File.WriteAllBytes(dataOvlPath, BuildDataOvl(new byte[] { 0, 2, 4, 7, 8, 10, 12, 14 }));

                MapDatParser parser = new MapDatParser();
                MapDatFile file = parser.ParseFile(datPath);

                Assert.Equal(MapDatKind.Towne, file.Kind);
                Assert.Equal(8, file.Sections.Count);
                Assert.Equal("Moonglow", file.Sections[0].Name);
                Assert.Equal(2, file.Sections[0].Planes.Count);
                Assert.Equal(3, file.Sections[2].Planes.Count);
                Assert.Equal(1, file.Sections[3].Planes.Count);
                Assert.Equal(14, file.Sections[7].Planes[0].GlobalMapIndex);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ParseBrit_ExpandsWaterChunksUsingDataOvlChunkTable()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string datPath = Path.Combine(tempDirectory, "BRIT.DAT");
                string dataOvlPath = Path.Combine(tempDirectory, "DATA.OVL");

                byte[] britDat = Enumerable.Repeat((byte)0x2A, 256).ToArray();
                File.WriteAllBytes(datPath, britDat);
                File.WriteAllBytes(dataOvlPath, BuildBritDataOvl());

                MapDatParser parser = new MapDatParser();
                MapDatFile file = parser.ParseFile(datPath);
                MapPlane plane = file.Sections[0].Planes[0];

                Assert.Equal(MapDatKind.Britannia, file.Kind);
                Assert.Equal(256, plane.Width);
                Assert.Equal(256, plane.Height);
                Assert.Equal(0x01, plane.Tiles[0]);
                Assert.Equal(0x2A, plane.Tiles[16]);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }

        private static byte[] BuildTowneDat()
        {
            byte[] bytes = new byte[16 * 1024];
            for (int planeIndex = 0; planeIndex < 16; planeIndex++)
            {
                for (int i = 0; i < 1024; i++)
                {
                    bytes[(planeIndex * 1024) + i] = (byte)planeIndex;
                }
            }

            return bytes;
        }

        private static byte[] BuildDataOvl(byte[] towneStartIndices)
        {
            byte[] bytes = new byte[0x4000];
            Array.Copy(towneStartIndices, 0, bytes, 0x1E2A, towneStartIndices.Length);
            return bytes;
        }

        private static byte[] BuildBritDataOvl()
        {
            byte[] bytes = new byte[0x4000];
            for (int i = 0; i < 0x100; i++)
            {
                bytes[0x3886 + i] = 0xFF;
            }

            bytes[0x3886 + 1] = 0x00;
            return bytes;
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"u5-map-{Guid.NewGuid():N}");
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

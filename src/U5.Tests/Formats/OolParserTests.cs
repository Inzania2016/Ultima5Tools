using U5.Core.Formats.Ool;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class OolParserTests
    {
        [Fact]
        public void ParseUnderOol_Exposes32EightByteRecords()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string path = Path.Combine(tempDirectory, "UNDER.OOL");
                File.WriteAllBytes(path, BuildSingleSegmentOol(23, 41, 41, 0x0E, 0xF2, 0xFF, 0x00, 0x00, 0x00));

                OolFile file = new OolParser().ParseFile(path);

                Assert.Equal(OolFileKind.Underworld, file.Kind);
                Assert.Single(file.Segments);
                Assert.Equal(32, file.Segments[0].Records.Count);

                OolRecord record = file.Segments[0].Records[23];
                Assert.Equal(41, record.PositionX);
                Assert.Equal(41, record.PositionY);
                Assert.Equal(0xF20E, record.RawWord23);
                Assert.Equal(0x00FF, record.RawWord45);
                Assert.Equal(0x0000, record.RawWord67);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }

        [Fact]
        public void ParseSavedOol_SplitsBritanniaAndUnderworldSegments()
        {
            string tempDirectory = CreateTempDirectory();
            try
            {
                string path = Path.Combine(tempDirectory, "SAVED.OOL");
                byte[] bytes = new byte[0x200];
                BuildRecord(bytes, 0, 3, 10, 20, 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF);
                BuildRecord(bytes, 0x100, 7, 30, 40, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66);
                File.WriteAllBytes(path, bytes);

                OolFile file = new OolParser().ParseFile(path);

                Assert.Equal(OolFileKind.Saved, file.Kind);
                Assert.Equal(2, file.Segments.Count);
                Assert.Equal("Britannia", file.Segments[0].Name);
                Assert.Equal("Underworld", file.Segments[1].Name);
                Assert.Equal(10, file.Segments[0].Records[3].PositionX);
                Assert.Equal(30, file.Segments[1].Records[7].PositionX);
                Assert.Equal(0x2211, file.Segments[1].Records[7].RawWord23);
            }
            finally
            {
                DeleteDirectory(tempDirectory);
            }
        }

        private static byte[] BuildSingleSegmentOol(int slotIndex, byte x, byte y, byte raw2, byte raw3, byte raw4, byte raw5, byte raw6, byte raw7)
        {
            byte[] bytes = new byte[0x100];
            BuildRecord(bytes, 0, slotIndex, x, y, raw2, raw3, raw4, raw5, raw6, raw7);
            return bytes;
        }

        private static void BuildRecord(byte[] bytes, int segmentOffset, int slotIndex, byte x, byte y, byte raw2, byte raw3, byte raw4, byte raw5, byte raw6, byte raw7)
        {
            int offset = segmentOffset + (slotIndex * OolParser.RecordSize);
            bytes[offset + 0] = x;
            bytes[offset + 1] = y;
            bytes[offset + 2] = raw2;
            bytes[offset + 3] = raw3;
            bytes[offset + 4] = raw4;
            bytes[offset + 5] = raw5;
            bytes[offset + 6] = raw6;
            bytes[offset + 7] = raw7;
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), $"u5-ool-{Guid.NewGuid():N}");
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

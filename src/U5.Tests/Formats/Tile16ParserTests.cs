using U5.Core.Formats.Tiles;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class Tile16ParserTests
    {
        [Fact]
        public void Parse_DecodesHighAndLowNibblesIntoPixels()
        {
            byte[] bytes = new byte[Tile16Parser.ExpectedFileLength];
            bytes[0] = 0x1F;
            bytes[1] = 0x23;

            Tile16File file = new Tile16Parser().Parse("TILES.16", bytes);
            Tile16Tile tile = file.Tiles[0];

            Assert.Equal(0x01, tile.Pixels[0]);
            Assert.Equal(0x0F, tile.Pixels[1]);
            Assert.Equal(0x02, tile.Pixels[2]);
            Assert.Equal(0x03, tile.Pixels[3]);
            Assert.Equal(16, file.Palette.Count);
            Assert.Equal("#0000AA", file.Palette[1].ToHexRgb());
        }

        [Fact]
        public void ExpandIfCompressed_InflatesUltimaStyleLzwPayload()
        {
            byte[] rawBytes = new byte[Tile16Parser.ExpectedFileLength];
            rawBytes[0] = 0x1F;
            rawBytes[1] = 0x23;
            rawBytes[2] = 0x45;
            rawBytes[3] = 0x67;

            byte[] compressedBytes = BuildWrappedLzwFile(rawBytes.Take(8).ToArray());
            byte[] expanded = new Tile16Parser().ExpandIfCompressed(compressedBytes);

            Assert.Equal(8, expanded.Length);
            Assert.Equal(rawBytes.Take(8).ToArray(), expanded);
        }

        private static byte[] BuildWrappedLzwFile(byte[] rawBytes)
        {
            List<int> codes = new List<int> { 256 };
            codes.AddRange(rawBytes.Select(value => (int)value));
            codes.Add(257);

            byte[] compressed = PackCodesLsb(codes, 9);
            byte[] fileBytes = new byte[4 + compressed.Length];
            BitConverter.GetBytes(rawBytes.Length).CopyTo(fileBytes, 0);
            Buffer.BlockCopy(compressed, 0, fileBytes, 4, compressed.Length);
            return fileBytes;
        }

        private static byte[] PackCodesLsb(IReadOnlyList<int> codes, int codeSize)
        {
            List<byte> bytes = new List<byte>();
            int bitOffset = 0;
            foreach (int code in codes)
            {
                for (int bitIndex = 0; bitIndex < codeSize; bitIndex++)
                {
                    int bit = (code >> bitIndex) & 0x01;
                    int absoluteBitIndex = bitOffset + bitIndex;
                    int byteIndex = absoluteBitIndex / 8;
                    int bitWithinByte = absoluteBitIndex % 8;
                    while (bytes.Count <= byteIndex)
                    {
                        bytes.Add(0);
                    }

                    if (bit != 0)
                    {
                        bytes[byteIndex] = (byte)(bytes[byteIndex] | (1 << bitWithinByte));
                    }
                }

                bitOffset += codeSize;
            }

            return bytes.ToArray();
        }
    }
}

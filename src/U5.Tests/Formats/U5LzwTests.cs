using U5.Core.Compression;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class U5LzwTests
    {
        [Fact]
        public void Decompress_DecodesLiteralStreamWithClearAndEndCodes()
        {
            byte[] compressed = PackCodesLsb(new[] { 256, 65, 66, 67, 257 }, 9);
            byte[] expanded = U5Lzw.Decompress(compressed, 3);

            Assert.Equal(new byte[] { 65, 66, 67 }, expanded);
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

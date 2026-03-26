using U5.Core.Formats.Tlk;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class TlkParserTests
    {
        [Fact]
        public void Parse_ParsesEntriesAndInferredSizes()
        {
            byte[] bytes = new byte[]
            {
                0x02, 0x00,
                0x01, 0x00, 0x0A, 0x00,
                0x02, 0x00, 0x0E, 0x00,
                0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF
            };

            TlkParser parser = new TlkParser();
            TlkFile tlk = parser.Parse(bytes);

            Assert.Equal((ushort)2, tlk.NpcCount);
            Assert.Equal(2, tlk.Entries.Count);
            Assert.Equal((ushort)1, tlk.Entries[0].NpcId);
            Assert.Equal((ushort)0x000A, tlk.Entries[0].BlockOffset);
            Assert.Equal(4, tlk.Entries[0].InferredBlockSize);
            Assert.Equal(2, tlk.Entries[1].InferredBlockSize);
        }
    }
}

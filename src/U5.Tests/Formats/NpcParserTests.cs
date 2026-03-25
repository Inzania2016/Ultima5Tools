using U5.Core.Formats.Npc;

namespace U5.Tests.Formats
{
    public sealed class NpcParserTests
    {
        [Fact]
        public void Parse_Parses256By18Layout()
        {
            byte[] bytes = new byte[NpcFile.ExpectedRecordCount * NpcFile.RecordSize];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(i % 256);
            }

            NpcParser parser = new NpcParser();
            NpcFile file = parser.Parse(bytes);

            Assert.True(file.IsExpectedLength);
            Assert.Equal(256, file.Records.Count);
            Assert.Equal(18, file.Records[0].RawBytes.Length);
            Assert.Equal(bytes[18], file.Records[1].RawBytes[0]);
        }
    }
}

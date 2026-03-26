using U5.Core.Formats.Tlk;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class TlkAnalysisServiceTests
    {
        [Fact]
        public void Analyze_MergesNonKeywordContinuationIntoPreviousKeywordAnswer()
        {
            byte[] block = Concat(
                EncodeAsciiZ("Mario"),
                EncodeAsciiZ("desc"),
                new byte[] { 0x00 },
                EncodeAsciiZ("job"),
                EncodeAsciiZ("bye"),
                EncodeAsciiZ("crim"),
                EncodeAsciiZ("Answer one"),
                new byte[] { 0xA2, 0x8D, 0x8D, 0x83, 0xA2, 0x00 },
                EncodeAsciiZ("caug"),
                new byte[] { 0x87, 0x00 },
                EncodeAsciiZ("tort"),
                EncodeAsciiZ("Don't ask!"),
                new byte[] { 0x90, 0x91 },
                EncodeAsciiZ("Prompt"));

            TlkFile file = new TlkFile
            {
                NpcCount = 1,
                Entries = new[]
                {
                    new TlkEntry { NpcId = 1, BlockOffset = 0x0006, InferredBlockSize = block.Length }
                },
                RawBytes = Concat(new byte[] { 0x01, 0x00, 0x01, 0x00, 0x06, 0x00 }, block)
            };

            TlkAnalysisService service = new TlkAnalysisService();
            TlkAnalysisReport report = service.Analyze(file);
            TlkNpcAnalysis npc = Assert.Single(report.NpcAnalyses);

            Assert.Equal(2, npc.KeywordGroups.Count);
            Assert.Equal(new[] { "crim" }, npc.KeywordGroups[0].Keywords);
            Assert.Contains("<QUOTE><NEWLINE><NEWLINE><PAUSE><QUOTE>", npc.KeywordGroups[0].AnswerText);
            Assert.Equal(new[] { "caug", "tort" }, npc.KeywordGroups[1].Keywords);
            Assert.Equal("Don't ask!", npc.KeywordGroups[1].AnswerText);
        }

        private static byte[] EncodeAsciiZ(string value)
        {
            byte[] bytes = new byte[value.Length + 1];
            for (int i = 0; i < value.Length; i++)
            {
                bytes[i] = (byte)(value[i] + 0x80);
            }

            bytes[^1] = 0x00;
            return bytes;
        }

        private static byte[] Concat(params byte[][] arrays)
        {
            int length = 0;
            foreach (byte[] array in arrays)
            {
                length += array.Length;
            }

            byte[] result = new byte[length];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
    }
}

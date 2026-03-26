using U5.Core.Formats.Tlk;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class TlkDisassemblerTests
    {
        [Fact]
        public void Disassemble_PreservesOrderedResponseActions()
        {
            TlkNpcAnalysis analysis = new TlkNpcAnalysis
            {
                EntryIndex = 1,
                NpcId = 2,
                BlockOffset = 0x100,
                BlockSize = 64,
                FixedEntries = Array.Empty<TlkFixedEntry>(),
                KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                TailSegments = new TlkTailSegment[]
                {
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0000,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x90, 0x91 }, EncodeAsciiZ("Wouldst thou like to try a bite?"))
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0010,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("Too bad.")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0020,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("y")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0022,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x86, 0x41 }, Concat(EncodeAsciiZ("Here thou art."), new byte[] { 0x92, 0x00 }))
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0030,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x90, 0x92 }, EncodeAsciiZ("Didst thou enjoy it?"))
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0040,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("I am deeply sorry!")
                    }
                }
            };

            TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis);

            Assert.Equal(2, disassembly.ScriptNodes.Count);
            TlkDisassembledNode firstNode = disassembly.ScriptNodes[0];
            Assert.Equal("LABEL_1", firstNode.Label);
            Assert.Single(firstNode.Responses);
            Assert.Equal("GIVE_ITEM(<TOK_41>)", firstNode.Responses[0].Actions[0]);
            Assert.Equal("TEXT \"Here thou art.\"", firstNode.Responses[0].Actions[1]);
            Assert.Equal("GOTO LABEL_2", firstNode.Responses[0].Actions[2]);
            Assert.DoesNotContain(disassembly.ScriptNodes, node => node.Label == "END_SENTINEL");
        }

        [Fact]
        public void Disassemble_FlagsGoldMismatchFromPromptAndAction()
        {
            TlkNpcAnalysis analysis = new TlkNpcAnalysis
            {
                EntryIndex = 2,
                NpcId = 3,
                BlockOffset = 0x200,
                BlockSize = 96,
                FixedEntries = Array.Empty<TlkFixedEntry>(),
                KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                TailSegments = new TlkTailSegment[]
                {
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0000,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x90, 0x91 }, EncodeAsciiZ("I'll tell thee more for 3 gold coins, o.k.?"))
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0010,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("Thy loss.")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0020,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("y")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0022,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x85, 0xB0, 0xB0, 0xB4 }, EncodeAsciiZ("We thank thee."))
                    }
                }
            };

            TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis);
            Assert.Contains(disassembly.Warnings, warning => warning.Contains("3 gold", StringComparison.Ordinal));
        }

        [Fact]
        public void Disassemble_DoesNotTreatResponseTokensAsTailRoutesInsideNodes()
        {
            TlkNpcAnalysis analysis = new TlkNpcAnalysis
            {
                EntryIndex = 3,
                NpcId = 4,
                BlockOffset = 0x300,
                BlockSize = 64,
                FixedEntries = Array.Empty<TlkFixedEntry>(),
                KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                TailSegments = new TlkTailSegment[]
                {
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0000,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("mant")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0005,
                        DecodedText = string.Empty,
                        RawBytes = new byte[] { 0x91, 0x00 }
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0007,
                        DecodedText = string.Empty,
                        RawBytes = Concat(new byte[] { 0x90, 0x91 }, EncodeAsciiZ("Dost thou seek?"))
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0015,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("No.")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x001A,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("y")
                    },
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x001C,
                        DecodedText = string.Empty,
                        RawBytes = EncodeAsciiZ("Yes.")
                    }
                }
            };

            TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis);
            Assert.Single(disassembly.TailRoutes);
            Assert.Equal("mant", disassembly.TailRoutes[0].Tokens[0]);
        }

        [Fact]
        public void Disassemble_RendersTailEndSentinelAsDedicatedElement()
        {
            TlkNpcAnalysis analysis = new TlkNpcAnalysis
            {
                EntryIndex = 5,
                NpcId = 6,
                BlockOffset = 0x500,
                BlockSize = 16,
                FixedEntries = Array.Empty<TlkFixedEntry>(),
                KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                TailSegments = new TlkTailSegment[]
                {
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0000,
                        DecodedText = string.Empty,
                        RawBytes = new byte[] { 0x90, 0x9F, 0xC0, 0x00 }
                    }
                }
            };

            TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis);
            TlkTailElement element = Assert.Single(disassembly.TailElements);
            TlkTailEndSentinelElement sentinel = Assert.IsType<TlkTailEndSentinelElement>(element);
            Assert.Equal("<END_SENTINEL>", sentinel.DisplayText);
        }

        [Fact]
        public void Disassemble_DoesNotConsumeLiteralTextAsSecondIfKnowsNameHandler()
        {
            TlkNpcAnalysis analysis = new TlkNpcAnalysis
            {
                EntryIndex = 4,
                NpcId = 5,
                BlockOffset = 0x400,
                BlockSize = 64,
                FixedEntries = Array.Empty<TlkFixedEntry>(),
                KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                TailSegments = new TlkTailSegment[]
                {
                    new TlkTailSegment
                    {
                        OffsetWithinBlock = 0x0000,
                        DecodedText = string.Empty,
                        RawBytes = new byte[] { 0x90, 0x91, 0x8C, 0x92, 0xC7, 0xEF, 0xEF, 0xE4, 0x00 }
                    }
                }
            };

            TlkNpcDisassembly disassembly = TlkDisassembler.Disassemble(analysis);
            TlkDisassembledNode node = Assert.Single(disassembly.ScriptNodes);
            Assert.Equal("Good", node.Prompt);
            Assert.Contains("IF_KNOWS_NAME(LABEL_2)", node.PromptActions);
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

        private static byte[] Concat(byte[] prefix, byte[] suffix)
        {
            byte[] output = new byte[prefix.Length + suffix.Length];
            Buffer.BlockCopy(prefix, 0, output, 0, prefix.Length);
            Buffer.BlockCopy(suffix, 0, output, prefix.Length, suffix.Length);
            return output;
        }
    }
}

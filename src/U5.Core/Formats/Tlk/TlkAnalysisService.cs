namespace U5.Core.Formats.Tlk
{
    public sealed class TlkAnalysisService
    {
        private static readonly string[] FixedFieldNames = new[]
        {
            "Name",
            "Description",
            "Greeting",
            "Job",
            "Bye"
        };

        public TlkAnalysisReport Analyze(TlkFile tlkFile, TlkDataOvlDictionary? dictionary = null, string? dataOvlPath = null)
        {
            List<TlkNpcAnalysis> npcAnalyses = new List<TlkNpcAnalysis>(tlkFile.Entries.Count);
            TlkTextDecoder decoder = new TlkTextDecoder(dictionary);

            bool offsetsStrictlyIncreasing = true;
            for (int i = 1; i < tlkFile.Entries.Count; i++)
            {
                if (tlkFile.Entries[i].BlockOffset <= tlkFile.Entries[i - 1].BlockOffset)
                {
                    offsetsStrictlyIncreasing = false;
                    break;
                }
            }

            int expectedHeaderSize = 2 + (tlkFile.NpcCount * 4);
            bool firstBlockMatchesExpectedHeaderSize = tlkFile.Entries.Count > 0
                && tlkFile.Entries[0].BlockOffset == expectedHeaderSize;

            for (int entryIndex = 0; entryIndex < tlkFile.Entries.Count; entryIndex++)
            {
                TlkEntry entry = tlkFile.Entries[entryIndex];
                int blockOffset = entry.BlockOffset;
                int blockSize = entry.InferredBlockSize ?? Math.Max(0, tlkFile.RawBytes.Length - blockOffset);

                if (blockOffset < 0 || blockOffset >= tlkFile.RawBytes.Length || blockSize <= 0)
                {
                    npcAnalyses.Add(new TlkNpcAnalysis
                    {
                        EntryIndex = entryIndex,
                        NpcId = entry.NpcId,
                        BlockOffset = blockOffset,
                        BlockSize = Math.Max(0, blockSize),
                        FixedEntries = Array.Empty<TlkFixedEntry>(),
                        KeywordGroups = Array.Empty<TlkKeywordGroup>(),
                        TailSegments = Array.Empty<TlkTailSegment>()
                    });

                    continue;
                }

                if (blockOffset + blockSize > tlkFile.RawBytes.Length)
                {
                    blockSize = tlkFile.RawBytes.Length - blockOffset;
                }

                byte[] blockBytes = tlkFile.RawBytes.Skip(blockOffset).Take(blockSize).ToArray();
                int position = 0;
                List<TlkFixedEntry> fixedEntries = new List<TlkFixedEntry>();
                foreach (string fixedFieldName in FixedFieldNames)
                {
                    if (position >= blockBytes.Length)
                    {
                        break;
                    }

                    int fieldOffset = position;
                    TlkDecodedString decoded = decoder.DecodeZeroTerminated(blockBytes, position);
                    if (decoded.BytesConsumed <= 0)
                    {
                        break;
                    }

                    fixedEntries.Add(new TlkFixedEntry
                    {
                        Name = fixedFieldName,
                        OffsetWithinBlock = fieldOffset,
                        DecodedText = decoded.DecodedText,
                        RawBytes = decoded.RawBytes
                    });

                    position += decoded.BytesConsumed;
                }

                List<TlkKeywordGroup> keywordGroups = new List<TlkKeywordGroup>();
                while (position < blockBytes.Length)
                {
                    if (LooksLikeTailStart(blockBytes, position))
                    {
                        break;
                    }

                    int groupOffset = position;
                    TlkDecodedString primaryKeyword = decoder.DecodeZeroTerminated(blockBytes, position);
                    if (primaryKeyword.BytesConsumed <= 0 || !LooksLikeKeyword(primaryKeyword.DecodedText))
                    {
                        break;
                    }

                    List<string> keywords = new List<string> { primaryKeyword.DecodedText };
                    position += primaryKeyword.BytesConsumed;

                    while (position + 1 < blockBytes.Length && blockBytes[position] == 0x87 && blockBytes[position + 1] == 0x00)
                    {
                        position += 2;
                        TlkDecodedString alternateKeyword = decoder.DecodeZeroTerminated(blockBytes, position);
                        if (alternateKeyword.BytesConsumed <= 0 || !LooksLikeKeyword(alternateKeyword.DecodedText))
                        {
                            position = Math.Max(groupOffset, position - 2);
                            break;
                        }

                        keywords.Add(alternateKeyword.DecodedText);
                        position += alternateKeyword.BytesConsumed;
                    }

                    if (position >= blockBytes.Length || LooksLikeTailStart(blockBytes, position))
                    {
                        position = groupOffset;
                        break;
                    }

                    TlkDecodedString answer = decoder.DecodeZeroTerminated(blockBytes, position);
                    if (answer.BytesConsumed <= 0)
                    {
                        position = groupOffset;
                        break;
                    }

                    int endExclusive = position + answer.BytesConsumed;
                    string answerText = answer.DecodedText;
                    position = endExclusive;

                    while (position < blockBytes.Length && !LooksLikeTailStart(blockBytes, position))
                    {
                        TlkDecodedString maybeContinuation = decoder.DecodeZeroTerminated(blockBytes, position);
                        if (maybeContinuation.BytesConsumed <= 0)
                        {
                            break;
                        }

                        if (LooksLikeKeyword(maybeContinuation.DecodedText))
                        {
                            break;
                        }

                        answerText += maybeContinuation.DecodedText;
                        endExclusive = position + maybeContinuation.BytesConsumed;
                        position = endExclusive;
                    }

                    byte[] rawGroupBytes = blockBytes.Skip(groupOffset).Take(endExclusive - groupOffset).ToArray();

                    keywordGroups.Add(new TlkKeywordGroup
                    {
                        OffsetWithinBlock = groupOffset,
                        Keywords = keywords,
                        AnswerText = answerText,
                        RawBytes = rawGroupBytes
                    });
                }

                List<TlkTailSegment> tailSegments = new List<TlkTailSegment>();
                while (position < blockBytes.Length)
                {
                    int tailOffset = position;
                    TlkDecodedString tailSegment = decoder.DecodeZeroTerminated(blockBytes, position);
                    if (tailSegment.BytesConsumed <= 0)
                    {
                        break;
                    }

                    if (tailSegment.RawBytes.Length == 1 && tailSegment.RawBytes[0] == 0x00)
                    {
                        position += tailSegment.BytesConsumed;
                        continue;
                    }

                    tailSegments.Add(new TlkTailSegment
                    {
                        OffsetWithinBlock = tailOffset,
                        DecodedText = tailSegment.DecodedText,
                        RawBytes = tailSegment.RawBytes
                    });

                    position += tailSegment.BytesConsumed;
                }

                npcAnalyses.Add(new TlkNpcAnalysis
                {
                    EntryIndex = entryIndex,
                    NpcId = entry.NpcId,
                    BlockOffset = blockOffset,
                    BlockSize = blockSize,
                    FixedEntries = fixedEntries,
                    KeywordGroups = keywordGroups,
                    TailSegments = tailSegments
                });
            }

            return new TlkAnalysisReport
            {
                NpcCount = tlkFile.NpcCount,
                FileSize = tlkFile.RawBytes.Length,
                ExpectedHeaderSize = expectedHeaderSize,
                OffsetsStrictlyIncreasing = offsetsStrictlyIncreasing,
                FirstBlockMatchesExpectedHeaderSize = firstBlockMatchesExpectedHeaderSize,
                DataOvlPath = dataOvlPath,
                DictionaryLoaded = dictionary is not null,
                NpcAnalyses = npcAnalyses
            };
        }

        private static bool LooksLikeKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length > 5)
            {
                return false;
            }

            foreach (char ch in text)
            {
                if (!char.IsLetterOrDigit(ch))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LooksLikeTailStart(byte[] blockBytes, int position)
        {
            if (position + 1 >= blockBytes.Length)
            {
                return false;
            }

            if (blockBytes[position] >= 0x91 && blockBytes[position] <= 0x9A && blockBytes[position + 1] == 0x00)
            {
                return true;
            }

            return blockBytes[position] == 0x90
                && blockBytes[position + 1] >= 0x91
                && blockBytes[position + 1] <= 0x9A;
        }
    }
}

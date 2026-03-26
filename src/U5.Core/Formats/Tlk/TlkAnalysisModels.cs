namespace U5.Core.Formats.Tlk
{
    public sealed class TlkFixedEntry
    {
        public required string Name { get; init; }

        public required int OffsetWithinBlock { get; init; }

        public required string DecodedText { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class TlkKeywordGroup
    {
        public required int OffsetWithinBlock { get; init; }

        public required IReadOnlyList<string> Keywords { get; init; }

        public required string AnswerText { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class TlkTailSegment
    {
        public required int OffsetWithinBlock { get; init; }

        public required string DecodedText { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class TlkNpcAnalysis
    {
        public required int EntryIndex { get; init; }

        public required ushort NpcId { get; init; }

        public required int BlockOffset { get; init; }

        public required int BlockSize { get; init; }

        public required IReadOnlyList<TlkFixedEntry> FixedEntries { get; init; }

        public required IReadOnlyList<TlkKeywordGroup> KeywordGroups { get; init; }

        public required IReadOnlyList<TlkTailSegment> TailSegments { get; init; }
    }

    public sealed class TlkAnalysisReport
    {
        public required ushort NpcCount { get; init; }

        public required int FileSize { get; init; }

        public required int ExpectedHeaderSize { get; init; }

        public required bool OffsetsStrictlyIncreasing { get; init; }

        public required bool FirstBlockMatchesExpectedHeaderSize { get; init; }

        public required string? DataOvlPath { get; init; }

        public required bool DictionaryLoaded { get; init; }

        public required IReadOnlyList<TlkNpcAnalysis> NpcAnalyses { get; init; }
    }

    public sealed class TlkDecodedString
    {
        public required string DecodedText { get; init; }

        public required int BytesConsumed { get; init; }

        public required byte[] RawBytes { get; init; }
    }
}

namespace U5.Core.Formats.Npc
{
    public sealed class NpcRecord
    {
        public required int Index { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class NpcFile
    {
        public const int ExpectedRecordCount = 256;

        public const int RecordSize = 18;

        public required IReadOnlyList<NpcRecord> Records { get; init; }

        public required bool IsExpectedLength { get; init; }

        public required byte[] RawBytes { get; init; }
    }
}

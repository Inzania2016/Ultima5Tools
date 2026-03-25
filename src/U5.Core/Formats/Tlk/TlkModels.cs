namespace U5.Core.Formats.Tlk
{
    public sealed class TlkEntry
    {
        public required ushort NpcId { get; init; }

        public required ushort BlockOffset { get; init; }

        public int? InferredBlockSize { get; set; }
    }

    public sealed class TlkFile
    {
        public required ushort NpcCount { get; init; }

        public required IReadOnlyList<TlkEntry> Entries { get; init; }

        public required byte[] RawBytes { get; init; }
    }
}

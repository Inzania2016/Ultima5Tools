namespace U5.Core.Formats.Ool
{
    public enum OolFileKind
    {
        Unknown,
        Britannia,
        Underworld,
        Initial,
        Saved
    }

    public sealed class OolRecord
    {
        public required int SlotIndex { get; init; }

        public required byte PositionX { get; init; }

        public required byte PositionY { get; init; }

        public required byte RawByte2 { get; init; }

        public required byte RawByte3 { get; init; }

        public required byte RawByte4 { get; init; }

        public required byte RawByte5 { get; init; }

        public required byte RawByte6 { get; init; }

        public required byte RawByte7 { get; init; }

        public bool IsEmpty =>
            PositionX == 0 &&
            PositionY == 0 &&
            RawByte2 == 0 &&
            RawByte3 == 0 &&
            RawByte4 == 0 &&
            RawByte5 == 0 &&
            RawByte6 == 0 &&
            RawByte7 == 0;

        public ushort RawWord23 => (ushort)(RawByte2 | (RawByte3 << 8));

        public ushort RawWord45 => (ushort)(RawByte4 | (RawByte5 << 8));

        public ushort RawWord67 => (ushort)(RawByte6 | (RawByte7 << 8));

        public string GetRawTailHex()
        {
            return $"{RawByte2:X2} {RawByte3:X2} {RawByte4:X2} {RawByte5:X2} {RawByte6:X2} {RawByte7:X2}";
        }
    }

    public sealed class OolSegment
    {
        public required int SegmentIndex { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<OolRecord> Records { get; init; }
    }

    public sealed class OolFile
    {
        public required string SourcePath { get; init; }

        public required string BaseName { get; init; }

        public required OolFileKind Kind { get; init; }

        public required int FileSize { get; init; }

        public required IReadOnlyList<OolSegment> Segments { get; init; }

        public required IReadOnlyList<string> Warnings { get; init; }
    }
}

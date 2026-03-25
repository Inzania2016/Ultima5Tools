namespace U5.Core.Formats.Gam
{
    public sealed class GamFile
    {
        public required string SourceName { get; init; }

        public required int Length { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class GamDiffRange
    {
        public required int StartOffset { get; init; }

        public required int Length { get; init; }

        public required byte[] LeftBytes { get; init; }

        public required byte[] RightBytes { get; init; }
    }

    public sealed class GamDiffResult
    {
        public required int LeftLength { get; init; }

        public required int RightLength { get; init; }

        public required IReadOnlyList<GamDiffRange> ChangedRanges { get; init; }
    }
}

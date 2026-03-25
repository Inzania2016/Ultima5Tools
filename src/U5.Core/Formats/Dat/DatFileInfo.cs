namespace U5.Core.Formats.Dat
{
    public sealed class DatFileInfo
    {
        public required string FullPath { get; init; }

        public required string BaseName { get; init; }

        public required string Extension { get; init; }

        public required long FileSize { get; init; }

        public required string HeuristicNote { get; init; }
    }
}

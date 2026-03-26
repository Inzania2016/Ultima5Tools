namespace U5.Core.Rendering
{
    public sealed class MapRenderRequest
    {
        public required string SourcePath { get; init; }

        public string? OutputDirectory { get; init; }
    }

    public sealed class MapRenderOutputFile
    {
        public required string FullPath { get; init; }

        public required string Kind { get; init; }

        public required string Description { get; init; }
    }

    public sealed class MapRenderResult
    {
        public required bool IsImplemented { get; init; }

        public required string Message { get; init; }

        public required IReadOnlyList<MapRenderOutputFile> OutputFiles { get; init; }
    }
}

namespace U5.Core.Rendering
{
    public sealed class MapRenderRequest
    {
        public required string SourcePath { get; init; }
    }

    public sealed class MapRenderResult
    {
        public required bool IsImplemented { get; init; }

        public required string Message { get; init; }
    }
}

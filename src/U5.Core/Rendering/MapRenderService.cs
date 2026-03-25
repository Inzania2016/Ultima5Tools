namespace U5.Core.Rendering
{
    public sealed class MapRenderService
    {
        public MapRenderResult Render(MapRenderRequest request)
        {
            return new MapRenderResult
            {
                IsImplemented = false,
                Message = $"Map rendering is not implemented yet. Received input: {request.SourcePath}"
            };
        }
    }
}

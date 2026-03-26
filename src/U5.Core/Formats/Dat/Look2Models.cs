namespace U5.Core.Formats.Dat
{
    public sealed class Look2File
    {
        public const int TileCount = 0x200;

        public required IReadOnlyList<string> Descriptions { get; init; }

        public string GetDescription(int tileId)
        {
            return tileId >= 0 && tileId < Descriptions.Count
                ? Descriptions[tileId]
                : string.Empty;
        }
    }
}

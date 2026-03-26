namespace U5.Core.Formats.Tiles
{
    public sealed class Tile16File
    {
        public required string SourcePath { get; init; }

        public required IReadOnlyList<Tile16Tile> Tiles { get; init; }

        public required IReadOnlyList<Tile16PaletteEntry> Palette { get; init; }
    }

    public sealed class Tile16Tile
    {
        public required int TileId { get; init; }

        public required int Width { get; init; }

        public required int Height { get; init; }

        public required byte[] Pixels { get; init; }
    }

    public sealed class Tile16PaletteEntry
    {
        public required byte Index { get; init; }

        public required byte Red { get; init; }

        public required byte Green { get; init; }

        public required byte Blue { get; init; }

        public string ToHexRgb()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }
    }
}

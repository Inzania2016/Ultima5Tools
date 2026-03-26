using U5.Core.Formats.Npc;

namespace U5.Core.Formats.Dat
{
    public enum MapDatKind
    {
        Unknown,
        Britannia,
        Underworld,
        Towne,
        Dwelling,
        Castle,
        Keep
    }

    public sealed class MapPlane
    {
        public required int GlobalMapIndex { get; init; }

        public required int PlaneIndexWithinSection { get; init; }

        public required string LevelLabel { get; init; }

        public required int Width { get; init; }

        public required int Height { get; init; }

        public required byte[] Tiles { get; init; }
    }

    public sealed class MapSection
    {
        public required int SectionIndex { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyList<MapPlane> Planes { get; init; }

        public required NpcMapBlock? NpcMap { get; init; }
    }

    public sealed class MapDatFile
    {
        public required string SourcePath { get; init; }

        public required string BaseName { get; init; }

        public required MapDatKind Kind { get; init; }

        public required IReadOnlyList<MapSection> Sections { get; init; }

        public required IReadOnlyList<string> Warnings { get; init; }
    }
}

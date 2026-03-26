using U5.Core.Formats.Npc;

namespace U5.Core.Formats.Ovl
{
    public sealed class DataOvlMapSetInfo
    {
        public required string DatName { get; init; }

        public required NpcFileKind Kind { get; init; }

        public required IReadOnlyList<string> SectionNames { get; init; }

        public required IReadOnlyList<byte> StartMapIndices { get; init; }
    }

    public sealed class DataOvlInfo
    {
        public required int FileSize { get; init; }

        public required IReadOnlyList<string> TlkTokens { get; init; }

        public required IReadOnlyList<string> CityNames { get; init; }

        public required IReadOnlyList<string> OtherLocationNames { get; init; }

        public required IReadOnlyList<DataOvlMapSetInfo> MapSets { get; init; }

        public DataOvlMapSetInfo? GetMapSet(NpcFileKind kind)
        {
            return MapSets.FirstOrDefault(set => set.Kind == kind);
        }
    }
}

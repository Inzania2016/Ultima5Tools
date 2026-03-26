namespace U5.Core.Formats.Npc
{
    public enum NpcFileKind
    {
        Unknown,
        Towne,
        Dwelling,
        Castle,
        Keep
    }

    public sealed class NpcScheduleStop
    {
        public required int StopIndex { get; init; }

        public required byte AiType { get; init; }

        public required byte X { get; init; }

        public required byte Y { get; init; }

        public required sbyte Z { get; init; }
    }

    public sealed class NpcSchedule
    {
        public required IReadOnlyList<NpcScheduleStop> Stops { get; init; }

        public required IReadOnlyList<byte> Times { get; init; }
    }

    public sealed class NpcSlot
    {
        public required int SlotIndex { get; init; }

        public required byte Type { get; init; }

        public required byte DialogNumber { get; init; }

        public required string DialogMeaning { get; init; }

        public required NpcSchedule Schedule { get; init; }

        public required bool HasAnyData { get; init; }

        public required byte[] RawBytes { get; init; }
    }

    public sealed class NpcMapBlock
    {
        public required int MapIndex { get; init; }

        public required string MapName { get; init; }

        public required IReadOnlyList<NpcSlot> Slots { get; init; }
    }

    public sealed class NpcFile
    {
        public const int MapCount = 8;
        public const int SlotsPerMap = 32;
        public const int ScheduleSize = 16;
        public const int MapBlockSize = 576;
        public const int ExpectedFileSize = MapCount * MapBlockSize;

        public required NpcFileKind Kind { get; init; }

        public required bool IsExpectedLength { get; init; }

        public required IReadOnlyList<NpcMapBlock> Maps { get; init; }

        public required byte[] RawBytes { get; init; }
    }
}

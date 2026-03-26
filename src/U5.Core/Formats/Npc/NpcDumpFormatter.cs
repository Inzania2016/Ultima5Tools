using System.Text;
using U5.Core.Formats.Ovl;
using U5.Core.Utilities;

namespace U5.Core.Formats.Npc
{
    public static class NpcDumpFormatter
    {
        public static string Format(NpcFile npcFile, DataOvlInfo? dataOvlInfo = null, string? dataOvlPath = null)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("NPC Schedule Analysis");
            writer.WriteLine($"Kind: {npcFile.Kind}");
            writer.WriteLine($"File Size: {npcFile.RawBytes.Length} bytes");
            writer.WriteLine($"Expected Layout: {NpcFile.MapCount} maps x {NpcFile.SlotsPerMap} slots x 18 bytes");
            writer.WriteLine($"Exact Expected Length: {npcFile.IsExpectedLength}");

            DataOvlMapSetInfo? mapSet = dataOvlInfo?.GetMapSet(npcFile.Kind);
            if (!string.IsNullOrWhiteSpace(dataOvlPath))
            {
                writer.WriteLine($"DATA.OVL: {dataOvlPath}");
            }

            writer.WriteLine();

            foreach (NpcMapBlock map in npcFile.Maps)
            {
                int activeCount = map.Slots.Count(slot => slot.HasAnyData);
                string startIndexSuffix = mapSet is not null && map.MapIndex < mapSet.StartMapIndices.Count
                    ? $" / Start Map Index {mapSet.StartMapIndices[map.MapIndex]}"
                    : string.Empty;

                writer.WriteLine($"=== Map {map.MapIndex:D2}: {map.MapName}{startIndexSuffix} ===");
                writer.WriteLine($"  Active Slots: {activeCount}");

                foreach (NpcSlot slot in map.Slots.Where(slot => slot.HasAnyData))
                {
                    writer.WriteLine($"  [{slot.SlotIndex:D2}] Type {slot.Type} (0x{slot.Type:X2}) / Dialog {slot.DialogNumber} - {slot.DialogMeaning}");
                    writer.WriteLine($"    Stops: {FormatStops(slot.Schedule.Stops)}");
                    writer.WriteLine($"    Times: {FormatTimes(slot.Schedule.Times)}");
                    writer.WriteLine($"    Raw: {HexFormatting.ToHex(slot.RawBytes)}");
                }

                writer.WriteLine();
            }

            return writer.ToString();
        }

        private static string FormatStops(IReadOnlyList<NpcScheduleStop> stops)
        {
            return string.Join(" | ", stops.Select(stop => $"#{stop.StopIndex} ai={stop.AiType} pos=({stop.X},{stop.Y},{stop.Z})"));
        }

        private static string FormatTimes(IReadOnlyList<byte> times)
        {
            if (times.Count < 4)
            {
                return string.Empty;
            }

            return $"{times[0]:D2}->stop0, {times[1]:D2}->stop1, {times[2]:D2}->stop2, {times[3]:D2}->stop1";
        }
    }
}

namespace U5.Core.Formats.Npc
{
    public sealed class NpcParser
    {
        public NpcFile ParseFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Parse(bytes, path);
        }

        public NpcFile Parse(byte[] bytes, string? sourcePath = null)
        {
            bool isExpectedLength = bytes.Length == NpcFile.ExpectedFileSize;
            int mapCount = bytes.Length / NpcFile.MapBlockSize;
            List<NpcMapBlock> maps = new List<NpcMapBlock>(mapCount);
            NpcFileKind kind = DetectKind(sourcePath);

            for (int mapIndex = 0; mapIndex < mapCount; mapIndex++)
            {
                int blockOffset = mapIndex * NpcFile.MapBlockSize;
                byte[] blockBytes = bytes.Skip(blockOffset).Take(NpcFile.MapBlockSize).ToArray();
                if (blockBytes.Length < NpcFile.MapBlockSize)
                {
                    break;
                }

                List<NpcSlot> slots = new List<NpcSlot>(NpcFile.SlotsPerMap);
                for (int slotIndex = 0; slotIndex < NpcFile.SlotsPerMap; slotIndex++)
                {
                    int scheduleOffset = slotIndex * NpcFile.ScheduleSize;
                    byte[] scheduleBytes = blockBytes.Skip(scheduleOffset).Take(NpcFile.ScheduleSize).ToArray();
                    byte type = blockBytes[(NpcFile.SlotsPerMap * NpcFile.ScheduleSize) + slotIndex];
                    byte dialog = blockBytes[(NpcFile.SlotsPerMap * NpcFile.ScheduleSize) + NpcFile.SlotsPerMap + slotIndex];

                    List<NpcScheduleStop> stops = new List<NpcScheduleStop>(3);
                    for (int stopIndex = 0; stopIndex < 3; stopIndex++)
                    {
                        stops.Add(new NpcScheduleStop
                        {
                            StopIndex = stopIndex,
                            AiType = scheduleBytes[stopIndex],
                            X = scheduleBytes[3 + stopIndex],
                            Y = scheduleBytes[6 + stopIndex],
                            Z = unchecked((sbyte)scheduleBytes[9 + stopIndex])
                        });
                    }

                    List<byte> times = scheduleBytes.Skip(12).Take(4).ToList();
                    byte[] rawBytes = scheduleBytes.Concat(new[] { type, dialog }).ToArray();

                    slots.Add(new NpcSlot
                    {
                        SlotIndex = slotIndex,
                        Type = type,
                        DialogNumber = dialog,
                        DialogMeaning = DescribeDialog(dialog),
                        Schedule = new NpcSchedule
                        {
                            Stops = stops,
                            Times = times
                        },
                        HasAnyData = rawBytes.Any(value => value != 0),
                        RawBytes = rawBytes
                    });
                }

                maps.Add(new NpcMapBlock
                {
                    MapIndex = mapIndex,
                    MapName = NpcMapCatalog.GetMapName(kind, mapIndex),
                    Slots = slots
                });
            }

            return new NpcFile
            {
                Kind = kind,
                IsExpectedLength = isExpectedLength,
                Maps = maps,
                RawBytes = bytes
            };
        }

        private static NpcFileKind DetectKind(string? sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return NpcFileKind.Unknown;
            }

            string fileName = Path.GetFileNameWithoutExtension(sourcePath).ToUpperInvariant();
            return fileName switch
            {
                "TOWNE" => NpcFileKind.Towne,
                "DWELLING" => NpcFileKind.Dwelling,
                "CASTLE" => NpcFileKind.Castle,
                "KEEP" => NpcFileKind.Keep,
                _ => NpcFileKind.Unknown
            };
        }

        private static string DescribeDialog(byte dialogNumber)
        {
            return dialogNumber switch
            {
                0 => "No response / silent NPC",
                129 => "Weapon dealer",
                130 => "Barkeeper",
                131 => "Horse seller",
                132 => "Ship seller",
                133 => "Magic seller",
                134 => "Guild master",
                135 => "Healer",
                136 => "Innkeeper",
                255 => "Guard / harassment script",
                < 129 => $"TLK entry {dialogNumber}",
                _ => $"Special dialog {dialogNumber}"
            };
        }
    }

    public static class NpcMapCatalog
    {
        private static readonly string[] TowneNames = new[]
        {
            "Moonglow",
            "Britain",
            "Jhelom",
            "Yew",
            "Minoc",
            "Trinsic",
            "Skara Brae",
            "New Magincia"
        };

        private static readonly string[] DwellingNames = new[]
        {
            "Fogsbane",
            "Stormcrow",
            "Greyhaven",
            "Waveguide",
            "Iolo's Hut",
            "Spektran",
            "Sin'Vraal's Hut",
            "Grendel's Hut"
        };

        private static readonly string[] CastleNames = new[]
        {
            "Lord British's Castle",
            "Blackthorn's Castle",
            "West Britanny",
            "North Britanny",
            "East Britanny",
            "Paws",
            "Cove",
            "Buccaneer's Den"
        };

        private static readonly string[] KeepNames = new[]
        {
            "Ararat",
            "Bordermarch",
            "Farthing",
            "Windemere",
            "Stonegate",
            "The Lycaeum",
            "Empath Abbey",
            "Serpent's Hold"
        };

        public static string GetMapName(NpcFileKind kind, int mapIndex)
        {
            return kind switch
            {
                NpcFileKind.Towne => GetName(TowneNames, mapIndex),
                NpcFileKind.Dwelling => GetName(DwellingNames, mapIndex),
                NpcFileKind.Castle => GetName(CastleNames, mapIndex),
                NpcFileKind.Keep => GetName(KeepNames, mapIndex),
                _ => $"Map {mapIndex}"
            };
        }

        private static string GetName(IReadOnlyList<string> names, int mapIndex)
        {
            return mapIndex >= 0 && mapIndex < names.Count ? names[mapIndex] : $"Map {mapIndex}";
        }
    }
}

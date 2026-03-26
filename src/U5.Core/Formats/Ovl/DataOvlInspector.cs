using U5.Core.Formats.Npc;
using U5.Core.Formats.Tlk;

namespace U5.Core.Formats.Ovl
{
    public sealed class DataOvlInspector
    {
        private const int TowneStartMapOffset = 0x1E2A;
        private const int DwellingStartMapOffset = 0x1E32;
        private const int CastleStartMapOffset = 0x1E3A;
        private const int KeepStartMapOffset = 0x1E42;
        private const int CityNameIndexOffset = 0x1E4A;
        private const int CityNameCount = 13;
        private const int OtherLocationNameIndexOffset = 0x1E6E;
        private const int OtherLocationNameCount = 22;
        private const int PointerBias = 0x10;

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

        public DataOvlInfo InspectFile(string path)
        {
            return Inspect(File.ReadAllBytes(path));
        }

        public DataOvlInfo Inspect(byte[] bytes)
        {
            TlkDataOvlDictionary dictionary = TlkDataOvlDictionary.Load(bytes);
            List<string> tlkTokens = Enumerable.Range(0, TlkDataOvlDictionary.TokenCount)
                .Select(index => dictionary.Resolve((byte)index))
                .ToList();

            List<string> cityNames = ReadIndexedNames(bytes, CityNameIndexOffset, CityNameCount);
            List<string> otherLocationNames = ReadIndexedNames(bytes, OtherLocationNameIndexOffset, OtherLocationNameCount);

            List<DataOvlMapSetInfo> mapSets = new List<DataOvlMapSetInfo>
            {
                new DataOvlMapSetInfo
                {
                    DatName = "TOWNE.DAT",
                    Kind = NpcFileKind.Towne,
                    SectionNames = TowneNames,
                    StartMapIndices = ReadByteArray(bytes, TowneStartMapOffset, 8)
                },
                new DataOvlMapSetInfo
                {
                    DatName = "DWELLING.DAT",
                    Kind = NpcFileKind.Dwelling,
                    SectionNames = DwellingNames,
                    StartMapIndices = ReadByteArray(bytes, DwellingStartMapOffset, 8)
                },
                new DataOvlMapSetInfo
                {
                    DatName = "CASTLE.DAT",
                    Kind = NpcFileKind.Castle,
                    SectionNames = CastleNames,
                    StartMapIndices = ReadByteArray(bytes, CastleStartMapOffset, 8)
                },
                new DataOvlMapSetInfo
                {
                    DatName = "KEEP.DAT",
                    Kind = NpcFileKind.Keep,
                    SectionNames = KeepNames,
                    StartMapIndices = ReadByteArray(bytes, KeepStartMapOffset, 8)
                }
            };

            return new DataOvlInfo
            {
                FileSize = bytes.Length,
                TlkTokens = tlkTokens,
                CityNames = cityNames,
                OtherLocationNames = otherLocationNames,
                MapSets = mapSets
            };
        }

        public static string? TryLocateSiblingDataOvl(string sourcePath)
        {
            string? directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string[] candidates = new[]
            {
                Path.Combine(directory, "DATA.OVL"),
                Path.Combine(directory, "data.ovl")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static List<byte> ReadByteArray(byte[] bytes, int offset, int count)
        {
            EnsureAvailable(bytes, offset, count);
            return bytes.Skip(offset).Take(count).ToList();
        }

        private static List<string> ReadIndexedNames(byte[] bytes, int offset, int count)
        {
            EnsureAvailable(bytes, offset, count * 2);
            List<string> names = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                ushort pointer = BitConverter.ToUInt16(bytes, offset + (i * 2));
                int textOffset = pointer + PointerBias;
                names.Add(ReadAsciiZ(bytes, textOffset));
            }

            return names;
        }

        private static string ReadAsciiZ(byte[] bytes, int offset)
        {
            if (offset < 0 || offset >= bytes.Length)
            {
                return string.Empty;
            }

            int terminator = Array.IndexOf(bytes, (byte)0x00, offset);
            if (terminator < 0)
            {
                terminator = bytes.Length;
            }

            int length = Math.Max(0, terminator - offset);
            return length == 0 ? string.Empty : System.Text.Encoding.Latin1.GetString(bytes, offset, length);
        }

        private static void EnsureAvailable(byte[] bytes, int offset, int count)
        {
            if (offset < 0 || offset + count > bytes.Length)
            {
                throw new InvalidDataException("DATA.OVL is too small for the requested table.");
            }
        }
    }
}

using U5.Core.Formats.Npc;
using U5.Core.Formats.Ovl;

namespace U5.Core.Formats.Dat
{
    public sealed class MapDatParser
    {
        private const int SettlementMapWidth = 32;
        private const int SettlementMapHeight = 32;
        private const int SettlementPlaneSize = SettlementMapWidth * SettlementMapHeight;
        private const int ChunkSize = 16;
        private const int ChunkTileCount = ChunkSize * ChunkSize;
        private const int ChunkingTableOffset = 0x3886;
        private const int ChunkingTableLength = 0x100;

        public MapDatFile ParseFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Map DAT file was not found.", path);
            }

            string sourcePath = fileInfo.FullName;
            string baseName = Path.GetFileNameWithoutExtension(fileInfo.Name).ToUpperInvariant();
            byte[] bytes = File.ReadAllBytes(sourcePath);
            string? siblingDataOvlPath = DataOvlInspector.TryLocateSiblingDataOvl(sourcePath);
            DataOvlInfo? dataOvl = siblingDataOvlPath is null ? null : new DataOvlInspector().InspectFile(siblingDataOvlPath);
            byte[]? dataOvlBytes = siblingDataOvlPath is null ? null : File.ReadAllBytes(siblingDataOvlPath);
            string? siblingNpcPath = TryLocateSiblingNpc(sourcePath);
            NpcFile? npcFile = siblingNpcPath is null ? null : new NpcParser().ParseFile(siblingNpcPath);

            MapDatKind kind = DetectKind(baseName);
            List<string> warnings = new List<string>();
            IReadOnlyList<MapSection> sections = kind switch
            {
                MapDatKind.Towne or MapDatKind.Dwelling or MapDatKind.Castle or MapDatKind.Keep => ParseSettlementMaps(kind, bytes, dataOvl, npcFile, warnings),
                MapDatKind.Underworld => new[] { ParseChunkedWorldSection("Underworld", bytes, null, warnings) },
                MapDatKind.Britannia => new[] { ParseChunkedWorldSection("Britannia", bytes, dataOvlBytes, warnings) },
                _ => Array.Empty<MapSection>()
            };

            if (sections.Count == 0)
            {
                warnings.Add("No map sections were parsed for this DAT file kind.");
            }

            return new MapDatFile
            {
                SourcePath = sourcePath,
                BaseName = baseName,
                Kind = kind,
                Sections = sections,
                Warnings = warnings
            };
        }

        public static string? TryLocateSiblingLook2Dat(string sourcePath)
        {
            string? directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string[] candidates = new[]
            {
                Path.Combine(directory, "LOOK2.DAT"),
                Path.Combine(directory, "look2.dat")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        public static string? TryLocateSiblingNpc(string sourcePath)
        {
            string? directory = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string[] candidates = new[]
            {
                Path.Combine(directory, $"{baseName}.NPC"),
                Path.Combine(directory, $"{baseName}.npc")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static MapDatKind DetectKind(string baseName)
        {
            return baseName switch
            {
                "BRIT" => MapDatKind.Britannia,
                "UNDER" => MapDatKind.Underworld,
                "TOWNE" => MapDatKind.Towne,
                "DWELLING" => MapDatKind.Dwelling,
                "CASTLE" => MapDatKind.Castle,
                "KEEP" => MapDatKind.Keep,
                _ => MapDatKind.Unknown
            };
        }

        private static IReadOnlyList<MapSection> ParseSettlementMaps(
            MapDatKind kind,
            byte[] bytes,
            DataOvlInfo? dataOvl,
            NpcFile? npcFile,
            List<string> warnings)
        {
            if (bytes.Length % SettlementPlaneSize != 0)
            {
                warnings.Add($"Settlement DAT length {bytes.Length} is not a multiple of {SettlementPlaneSize} bytes.");
            }

            int planeCount = bytes.Length / SettlementPlaneSize;
            DataOvlMapSetInfo? mapSet = dataOvl?.GetMapSet(ToNpcKind(kind));
            IReadOnlyList<byte> startIndices = mapSet?.StartMapIndices ?? BuildFallbackStartIndices(planeCount, 8);
            IReadOnlyList<string> sectionNames = mapSet?.SectionNames ?? Enumerable.Range(0, 8).Select(index => $"Section {index}").ToList();
            List<MapSection> sections = new List<MapSection>(sectionNames.Count);

            for (int sectionIndex = 0; sectionIndex < sectionNames.Count; sectionIndex++)
            {
                int start = sectionIndex < startIndices.Count ? startIndices[sectionIndex] : planeCount;
                int end = sectionIndex + 1 < startIndices.Count ? startIndices[sectionIndex + 1] : planeCount;
                if (sectionIndex == sectionNames.Count - 1)
                {
                    end = planeCount;
                }

                if (start > planeCount)
                {
                    warnings.Add($"Section {sectionNames[sectionIndex]} starts beyond the available plane count ({start} > {planeCount}).");
                    start = planeCount;
                }

                end = Math.Clamp(end, start, planeCount);
                int sectionPlaneCount = end - start;
                List<MapPlane> planes = new List<MapPlane>(sectionPlaneCount);
                IReadOnlyList<string> labels = BuildSettlementLevelLabels(sectionPlaneCount);

                for (int planeIndex = 0; planeIndex < sectionPlaneCount; planeIndex++)
                {
                    int globalIndex = start + planeIndex;
                    byte[] tiles = bytes.Skip(globalIndex * SettlementPlaneSize).Take(SettlementPlaneSize).ToArray();
                    planes.Add(new MapPlane
                    {
                        GlobalMapIndex = globalIndex,
                        PlaneIndexWithinSection = planeIndex,
                        LevelLabel = planeIndex < labels.Count ? labels[planeIndex] : $"Map {globalIndex}",
                        Width = SettlementMapWidth,
                        Height = SettlementMapHeight,
                        Tiles = tiles
                    });
                }

                NpcMapBlock? npcMap = npcFile is not null && sectionIndex < npcFile.Maps.Count
                    ? npcFile.Maps[sectionIndex]
                    : null;

                sections.Add(new MapSection
                {
                    SectionIndex = sectionIndex,
                    Name = sectionNames[sectionIndex],
                    Planes = planes,
                    NpcMap = npcMap
                });
            }

            return sections;
        }

        private static MapSection ParseChunkedWorldSection(string name, byte[] bytes, byte[]? dataOvlBytes, List<string> warnings)
        {
            byte[] fullMap = new byte[256 * 256];

            if (name.Equals("Underworld", StringComparison.OrdinalIgnoreCase))
            {
                if (bytes.Length != fullMap.Length)
                {
                    warnings.Add($"UNDER.DAT expected {fullMap.Length} bytes but found {bytes.Length} bytes.");
                }

                Array.Copy(bytes, fullMap, Math.Min(bytes.Length, fullMap.Length));
            }
            else
            {
                if (dataOvlBytes is null)
                {
                    throw new InvalidDataException("BRIT.DAT rendering needs sibling DATA.OVL for the chunking table.");
                }

                if (dataOvlBytes.Length < ChunkingTableOffset + ChunkingTableLength)
                {
                    throw new InvalidDataException("DATA.OVL does not contain the Britannia chunking table.");
                }

                byte[] chunkingTable = dataOvlBytes.Skip(ChunkingTableOffset).Take(ChunkingTableLength).ToArray();
                for (int chunkIndex = 0; chunkIndex < ChunkingTableLength; chunkIndex++)
                {
                    int chunkX = chunkIndex % 16;
                    int chunkY = chunkIndex / 16;
                    byte chunkPointer = chunkingTable[chunkIndex];
                    byte[] chunk = chunkPointer == 0xFF
                        ? Enumerable.Repeat((byte)0x01, ChunkTileCount).ToArray()
                        : bytes.Skip(chunkPointer * ChunkTileCount).Take(ChunkTileCount).ToArray();

                    if (chunk.Length != ChunkTileCount)
                    {
                        warnings.Add($"Chunk {chunkIndex} points beyond BRIT.DAT contents (pointer {chunkPointer}).");
                        chunk = Enumerable.Repeat((byte)0x00, ChunkTileCount).ToArray();
                    }

                    BlitChunk(fullMap, chunk, chunkX * ChunkSize, chunkY * ChunkSize);
                }
            }

            return new MapSection
            {
                SectionIndex = 0,
                Name = name,
                Planes = new[]
                {
                    new MapPlane
                    {
                        GlobalMapIndex = 0,
                        PlaneIndexWithinSection = 0,
                        LevelLabel = name,
                        Width = 256,
                        Height = 256,
                        Tiles = fullMap
                    }
                },
                NpcMap = null
            };
        }

        private static void BlitChunk(byte[] target, byte[] chunk, int startX, int startY)
        {
            for (int y = 0; y < ChunkSize; y++)
            {
                for (int x = 0; x < ChunkSize; x++)
                {
                    int targetIndex = ((startY + y) * 256) + (startX + x);
                    int chunkIndex = (y * ChunkSize) + x;
                    target[targetIndex] = chunk[chunkIndex];
                }
            }
        }

        private static IReadOnlyList<byte> BuildFallbackStartIndices(int planeCount, int sectionCount)
        {
            int stride = sectionCount == 0 ? 0 : Math.Max(1, planeCount / Math.Max(1, sectionCount));
            List<byte> starts = new List<byte>(sectionCount);
            for (int i = 0; i < sectionCount; i++)
            {
                starts.Add((byte)Math.Min(planeCount, i * stride));
            }

            return starts;
        }

        private static IReadOnlyList<string> BuildSettlementLevelLabels(int planeCount)
        {
            if (planeCount <= 0)
            {
                return Array.Empty<string>();
            }

            if (planeCount == 5)
            {
                return new[] { "Level -1", "Level 1", "Level 2", "Level 3", "Level 4" };
            }

            return Enumerable.Range(1, planeCount)
                .Select(level => $"Level {level}")
                .ToList();
        }

        private static NpcFileKind ToNpcKind(MapDatKind kind)
        {
            return kind switch
            {
                MapDatKind.Towne => NpcFileKind.Towne,
                MapDatKind.Dwelling => NpcFileKind.Dwelling,
                MapDatKind.Castle => NpcFileKind.Castle,
                MapDatKind.Keep => NpcFileKind.Keep,
                _ => NpcFileKind.Unknown
            };
        }
    }
}

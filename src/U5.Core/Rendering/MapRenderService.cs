using System.Security;
using System.Text;
using U5.Core.Formats.Dat;
using U5.Core.Formats.Npc;
using U5.Core.Formats.Ool;
using U5.Core.Formats.Ovl;
using U5.Core.Formats.Tlk;
using U5.Core.Formats.Tiles;

namespace U5.Core.Rendering
{
    public sealed class MapRenderService
    {
        public MapRenderResult Render(MapRenderRequest request)
        {
            MapDatParser parser = new MapDatParser();
            MapDatFile mapFile = parser.ParseFile(request.SourcePath);
            if (mapFile.Kind == MapDatKind.Unknown || mapFile.Sections.Count == 0)
            {
                return new MapRenderResult
                {
                    IsImplemented = false,
                    Message = $"Map rendering is not implemented for {Path.GetFileName(request.SourcePath)} yet.",
                    OutputFiles = Array.Empty<MapRenderOutputFile>()
                };
            }

            string outputDirectory = ResolveOutputDirectory(request);
            Directory.CreateDirectory(outputDirectory);

            Look2File? look2 = TryLoadLook2(request.SourcePath);
            Tile16File? tileSet = TryLoadTiles16(request.SourcePath);
            IReadOnlyDictionary<int, NpcIdentityInfo> npcIdentities = TryLoadNpcIdentities(request.SourcePath);
            WorldObjectOverlayInfo? worldObjects = TryLoadWorldObjects(mapFile, request.SourcePath);
            List<MapRenderOutputFile> outputFiles = new List<MapRenderOutputFile>();

            foreach (MapSection section in mapFile.Sections.Where(section => section.Planes.Count > 0))
            {
                string safeSectionName = Slugify(section.Name);
                string svgPath = Path.Combine(outputDirectory, $"{mapFile.BaseName}_{section.SectionIndex:D2}_{safeSectionName}.svg");
                File.WriteAllText(svgPath, RenderSectionSvg(mapFile, section, look2, tileSet, npcIdentities, worldObjects), Encoding.UTF8);
                outputFiles.Add(new MapRenderOutputFile
                {
                    FullPath = svgPath,
                    Kind = "svg",
                    Description = $"Rendered map section for {section.Name}"
                });

                if (section.NpcMap is not null)
                {
                    string npcSummaryPath = Path.Combine(outputDirectory, $"{mapFile.BaseName}_{section.SectionIndex:D2}_{safeSectionName}_npc.txt");
                    File.WriteAllText(npcSummaryPath, RenderNpcSummary(mapFile, section, npcIdentities), Encoding.UTF8);
                    outputFiles.Add(new MapRenderOutputFile
                    {
                        FullPath = npcSummaryPath,
                        Kind = "npc-summary",
                        Description = $"NPC schedule summary for {section.Name}"
                    });
                }

                if (worldObjects is not null && (mapFile.Kind == MapDatKind.Britannia || mapFile.Kind == MapDatKind.Underworld))
                {
                    string objectSummaryPath = Path.Combine(outputDirectory, $"{mapFile.BaseName}_{section.SectionIndex:D2}_{safeSectionName}_objects.txt");
                    File.WriteAllText(objectSummaryPath, RenderWorldObjectSummary(mapFile, section, worldObjects, look2), Encoding.UTF8);
                    outputFiles.Add(new MapRenderOutputFile
                    {
                        FullPath = objectSummaryPath,
                        Kind = "object-summary",
                        Description = $"OOL/world-object summary for {section.Name}"
                    });
                }
            }

            List<string> renderWarnings = new List<string>(mapFile.Warnings);
            if (worldObjects is not null)
            {
                foreach (string warning in worldObjects.File.Warnings)
                {
                    renderWarnings.Add($"OOL: {warning}");
                }
            }

            string manifestPath = Path.Combine(outputDirectory, $"{mapFile.BaseName}_manifest.txt");
            File.WriteAllText(manifestPath, RenderManifest(mapFile, outputFiles, renderWarnings), Encoding.UTF8);
            outputFiles.Add(new MapRenderOutputFile
            {
                FullPath = manifestPath,
                Kind = "manifest",
                Description = "Render manifest"
            });

            StringBuilder message = new StringBuilder();
            int svgCount = outputFiles.Count(file => file.Kind == "svg");
            int npcSummaryCount = outputFiles.Count(file => file.Kind == "npc-summary");
            int objectSummaryCount = outputFiles.Count(file => file.Kind == "object-summary");
            message.AppendLine($"Rendered {svgCount} map image(s) to {outputDirectory}");
            if (npcSummaryCount > 0)
            {
                message.AppendLine($"Wrote {npcSummaryCount} NPC schedule summary file(s).");
            }

            if (objectSummaryCount > 0)
            {
                message.AppendLine($"Wrote {objectSummaryCount} OOL/world-object summary file(s).");
            }

            foreach (string warning in renderWarnings)
            {
                message.AppendLine($"Warning: {warning}");
            }

            return new MapRenderResult
            {
                IsImplemented = true,
                Message = message.ToString().TrimEnd(),
                OutputFiles = outputFiles
            };
        }


        private static WorldObjectOverlayInfo? TryLoadWorldObjects(MapDatFile mapFile, string sourcePath)
        {
            if (mapFile.Kind != MapDatKind.Britannia && mapFile.Kind != MapDatKind.Underworld)
            {
                return null;
            }

            string? oolPath = OolParser.TryLocateSiblingOol(sourcePath);
            if (oolPath is null)
            {
                return null;
            }

            try
            {
                OolFile file = new OolParser().ParseFile(oolPath);
                OolSegment? segment = mapFile.Kind switch
                {
                    MapDatKind.Britannia => file.Segments.FirstOrDefault(current => string.Equals(current.Name, "Britannia", StringComparison.OrdinalIgnoreCase)) ?? file.Segments.FirstOrDefault(),
                    MapDatKind.Underworld => file.Segments.FirstOrDefault(current => string.Equals(current.Name, "Underworld", StringComparison.OrdinalIgnoreCase)) ?? file.Segments.LastOrDefault(),
                    _ => null
                };

                if (segment is null)
                {
                    return null;
                }

                return new WorldObjectOverlayInfo
                {
                    File = file,
                    Segment = segment
                };
            }
            catch
            {
                return null;
            }
        }

        private static Look2File? TryLoadLook2(string sourcePath)
        {
            string? look2Path = MapDatParser.TryLocateSiblingLook2Dat(sourcePath);
            if (look2Path is null)
            {
                return null;
            }

            return new Look2Parser().ParseFile(look2Path);
        }

        private static Tile16File? TryLoadTiles16(string sourcePath)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Directory.GetCurrentDirectory();
            string tilesPath = Path.Combine(directory, "TILES.16");
            if (!File.Exists(tilesPath))
            {
                return null;
            }

            try
            {
                return new Tile16Parser().ParseFile(tilesPath);
            }
            catch
            {
                return null;
            }
        }

        private static string ResolveOutputDirectory(MapRenderRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.OutputDirectory))
            {
                return Path.GetFullPath(request.OutputDirectory);
            }

            string sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(request.SourcePath)) ?? Directory.GetCurrentDirectory();
            string candidate = Path.GetFullPath(Path.Combine(sourceDirectory, "..", "output", "maps", Path.GetFileNameWithoutExtension(request.SourcePath).ToUpperInvariant()));
            return candidate;
        }

        private static string RenderSectionSvg(MapDatFile mapFile, MapSection section, Look2File? look2, Tile16File? tileSet, IReadOnlyDictionary<int, NpcIdentityInfo> npcIdentities, WorldObjectOverlayInfo? worldObjects)
        {
            int tileSize = section.Planes[0].Width >= 256 ? 4 : 16;
            int margin = 24;
            int sectionTitleHeight = 20;
            bool hasNpcLegend = section.NpcMap is not null;
            bool hasWorldObjectLegend = worldObjects is not null && worldObjects.Segment.Records.Any(record => !record.IsEmpty);
            int headerHeight = 34;
            if (hasNpcLegend)
            {
                headerHeight += 44;
            }

            if (hasWorldObjectLegend)
            {
                headerHeight += 44;
            }
            int panelGap = 24;
            int panelWidth = (section.Planes[0].Width * tileSize) + 2;
            int panelHeight = (section.Planes[0].Height * tileSize) + 2;
            int svgWidth = (margin * 2) + (section.Planes.Count * panelWidth) + ((section.Planes.Count - 1) * panelGap);
            int svgHeight = margin + sectionTitleHeight + headerHeight + panelHeight + margin;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{svgWidth}\" height=\"{svgHeight}\" viewBox=\"0 0 {svgWidth} {svgHeight}\">");
            sb.AppendLine("  <style>");
            sb.AppendLine("    text { font-family: Consolas, 'Courier New', monospace; fill: #222; }");
            sb.AppendLine("    .title { font-size: 16px; font-weight: bold; }");
            sb.AppendLine("    .subtitle { font-size: 12px; fill: #555; }");
            sb.AppendLine("    .panel-title { font-size: 12px; font-weight: bold; }");
            sb.AppendLine("    .legend { font-size: 11px; fill: #333; }");
            sb.AppendLine("    .npc-label { font-size: 8px; font-weight: bold; fill: #111; }");
            sb.AppendLine("    .tile-use { shape-rendering: crispEdges; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("  <rect x=\"0\" y=\"0\" width=\"100%\" height=\"100%\" fill=\"#f8f4ea\" />");
            sb.AppendLine($"  <text class=\"title\" x=\"{margin}\" y=\"{margin}\">{Escape(mapFile.BaseName)} :: {Escape(section.Name)}</text>");
            sb.AppendLine($"  <text class=\"subtitle\" x=\"{margin}\" y=\"{margin + 18}\">{Escape(BuildSectionSubtitle(mapFile, section, tileSet is not null, worldObjects is not null))}</text>");

            if (tileSet is not null)
            {
                IReadOnlyCollection<int> usedTileIds = section.Planes
                    .SelectMany(plane => plane.Tiles.Select(tileId => (int)tileId))
                    .Where(tileId => tileId >= 0 && tileId < tileSet.Tiles.Count)
                    .Distinct()
                    .OrderBy(tileId => tileId)
                    .ToArray();

                RenderTileDefinitions(sb, tileSet, usedTileIds);
            }

            int legendY = margin + sectionTitleHeight + 18;
            if (hasNpcLegend)
            {
                RenderNpcLegend(sb, section, margin, legendY, tileSize);
                legendY += 44;
            }

            if (hasWorldObjectLegend && worldObjects is not null)
            {
                RenderWorldObjectLegend(sb, worldObjects, margin, legendY);
            }

            for (int planeIndex = 0; planeIndex < section.Planes.Count; planeIndex++)
            {
                MapPlane plane = section.Planes[planeIndex];
                int panelX = margin + (planeIndex * (panelWidth + panelGap));
                int panelY = margin + sectionTitleHeight + headerHeight;
                int mapX = panelX + 1;
                int mapY = panelY + 1;

                sb.AppendLine($"  <text class=\"panel-title\" x=\"{panelX}\" y=\"{margin + sectionTitleHeight + headerHeight - 12}\">{Escape(plane.LevelLabel)} (Map {plane.GlobalMapIndex})</text>");
                sb.AppendLine($"  <rect x=\"{panelX}\" y=\"{panelY}\" width=\"{panelWidth}\" height=\"{panelHeight}\" fill=\"#111\" />");

                for (int y = 0; y < plane.Height; y++)
                {
                    for (int x = 0; x < plane.Width; x++)
                    {
                        int tileId = plane.Tiles[(y * plane.Width) + x];
                        string description = look2?.GetDescription(tileId) ?? string.Empty;
                        string title = $"Tile {tileId:X2} at ({x},{y})";
                        if (!string.IsNullOrWhiteSpace(description))
                        {
                            title += $": {description}";
                        }

                        int tileX = mapX + (x * tileSize);
                        int tileY = mapY + (y * tileSize);
                        if (tileSet is not null && tileId >= 0 && tileId < tileSet.Tiles.Count)
                        {
                            sb.AppendLine($"  <g><title>{Escape(title)}</title><use class=\"tile-use\" href=\"#tile-{tileId:X4}\" x=\"{tileX}\" y=\"{tileY}\" width=\"{tileSize}\" height=\"{tileSize}\" /></g>");
                        }
                        else
                        {
                            string fill = GetTileColor(tileId, description);
                            sb.AppendLine($"  <rect x=\"{tileX}\" y=\"{tileY}\" width=\"{tileSize}\" height=\"{tileSize}\" fill=\"{fill}\"><title>{Escape(title)}</title></rect>");
                        }
                    }
                }

                RenderNpcOverlay(sb, section, plane, mapX, mapY, tileSize, npcIdentities);
                RenderWorldObjectOverlay(sb, plane, mapX, mapY, tileSize, look2, worldObjects);
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }

        private static void RenderNpcLegend(StringBuilder sb, MapSection section, int x, int y, int tileSize)
        {
            int activeCount = section.NpcMap?.Slots.Count(slot => slot.HasAnyData && slot.SlotIndex > 0) ?? 0;
            sb.AppendLine($"  <text class=\"legend\" x=\"{x}\" y=\"{y}\">NPC overlay: {activeCount} active slot(s)</text>");
            sb.AppendLine($"  <line x1=\"{x}\" y1=\"{y + 10}\" x2=\"{x + 18}\" y2=\"{y + 10}\" stroke=\"#444\" stroke-width=\"1.5\" stroke-dasharray=\"3 2\" />");
            sb.AppendLine($"  <text class=\"legend\" x=\"{x + 24}\" y=\"{y + 14}\">route</text>");
            RenderLegendDot(sb, x + 88, y + 10, "#d1495b", "stop 0");
            RenderLegendDot(sb, x + 150, y + 10, "#edae49", "stop 1");
            RenderLegendDot(sb, x + 212, y + 10, "#00798c", "stop 2");
            sb.AppendLine($"  <text class=\"legend\" x=\"{x}\" y=\"{y + 32}\">labels show slot + TLK identity near stop 0; see *_npc.txt for the full schedule table</text>");
        }

        private static void RenderLegendDot(StringBuilder sb, int x, int y, string fill, string label)
        {
            sb.AppendLine($"  <circle cx=\"{x}\" cy=\"{y}\" r=\"4\" fill=\"{fill}\" stroke=\"#111\" stroke-width=\"0.5\" />");
            sb.AppendLine($"  <text class=\"legend\" x=\"{x + 8}\" y=\"{y + 4}\">{Escape(label)}</text>");
        }

        private static void RenderNpcOverlay(StringBuilder sb, MapSection section, MapPlane plane, int mapX, int mapY, int tileSize, IReadOnlyDictionary<int, NpcIdentityInfo> npcIdentities)
        {
            if (section.NpcMap is null)
            {
                return;
            }

            bool hasBasement = section.Planes.Count == 5;
            int markerRadius = Math.Max(2, tileSize / 4);
            int labelOffsetX = Math.Max(4, tileSize / 3);
            int labelOffsetY = Math.Max(4, tileSize / 3);

            foreach (NpcSlot slot in section.NpcMap.Slots.Where(slot => slot.HasAnyData && slot.SlotIndex > 0))
            {
                List<(NpcScheduleStop Stop, int StopIndex, int CenterX, int CenterY)> visibleStops = new List<(NpcScheduleStop Stop, int StopIndex, int CenterX, int CenterY)>();
                for (int stopIndex = 0; stopIndex < slot.Schedule.Stops.Count; stopIndex++)
                {
                    NpcScheduleStop stop = slot.Schedule.Stops[stopIndex];
                    int planeIndex = ResolveNpcPlaneIndex(stop.Z, hasBasement);
                    if (planeIndex != plane.PlaneIndexWithinSection)
                    {
                        continue;
                    }

                    if (stop.X >= plane.Width || stop.Y >= plane.Height)
                    {
                        continue;
                    }

                    int centerX = mapX + (stop.X * tileSize) + (tileSize / 2);
                    int centerY = mapY + (stop.Y * tileSize) + (tileSize / 2);
                    visibleStops.Add((stop, stopIndex, centerX, centerY));
                }

                if (visibleStops.Count == 0)
                {
                    continue;
                }

                NpcIdentityInfo? identity = ResolveNpcIdentity(slot, npcIdentities);

                if (visibleStops.Count > 1)
                {
                    string routePoints = string.Join(" ", visibleStops.Select(stop => $"{stop.CenterX},{stop.CenterY}"));
                    string routeTitle = BuildNpcRouteTitle(slot, visibleStops, identity);
                    sb.AppendLine($"  <polyline points=\"{routePoints}\" fill=\"none\" stroke=\"#3b3b3b\" stroke-width=\"1.5\" stroke-dasharray=\"3 2\" stroke-opacity=\"0.85\"><title>{Escape(routeTitle)}</title></polyline>");
                }

                foreach ((NpcScheduleStop stop, int stopIndex, int centerX, int centerY) in visibleStops)
                {
                    string fill = stopIndex switch
                    {
                        0 => "#d1495b",
                        1 => "#edae49",
                        _ => "#00798c"
                    };

                    string title = BuildNpcStopTitle(slot, stop, stopIndex, identity);
                    sb.AppendLine($"  <circle cx=\"{centerX}\" cy=\"{centerY}\" r=\"{markerRadius}\" fill=\"{fill}\" fill-opacity=\"0.82\" stroke=\"#111\" stroke-width=\"0.6\"><title>{Escape(title)}</title></circle>");

                    if (stopIndex == 0)
                    {
                        int labelX = centerX + labelOffsetX;
                        int labelY = centerY - labelOffsetY;
                        sb.AppendLine($"  <text class=\"npc-label\" x=\"{labelX}\" y=\"{labelY}\">{Escape(BuildNpcLabel(slot, identity))}</text>");
                    }
                }
            }
        }

        private static string BuildNpcRouteTitle(NpcSlot slot, IReadOnlyList<(NpcScheduleStop Stop, int StopIndex, int CenterX, int CenterY)> visibleStops, NpcIdentityInfo? identity)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"NPC slot {slot.SlotIndex} route");
            if (identity is not null)
            {
                sb.Append($" • {identity.DisplayName} (TLK {identity.NpcId})");
            }

            sb.Append($" • dialog {slot.DialogNumber} ({slot.DialogMeaning})");
            sb.Append(" • stops ");
            sb.Append(string.Join(" -> ", visibleStops.Select(stop => $"{stop.StopIndex}@({stop.Stop.X},{stop.Stop.Y},{stop.Stop.Z})")));
            return sb.ToString();
        }

        private static string BuildNpcStopTitle(NpcSlot slot, NpcScheduleStop stop, int stopIndex, NpcIdentityInfo? identity)
        {
            string timeText = stopIndex < slot.Schedule.Times.Count
                ? $" timeByte 0x{slot.Schedule.Times[stopIndex]:X2}"
                : string.Empty;

            string identityText = identity is null ? string.Empty : $" • {identity.DisplayName} (TLK {identity.NpcId})";
            return $"NPC slot {slot.SlotIndex} stop {stopIndex} @ ({stop.X},{stop.Y},{stop.Z}) ai 0x{stop.AiType:X2}{timeText} • type {slot.Type} • dialog {slot.DialogNumber} {slot.DialogMeaning}{identityText}";
        }

        private static int ResolveNpcPlaneIndex(sbyte z, bool hasBasement)
        {
            if (hasBasement)
            {
                return z < 0 ? 0 : z + 1;
            }

            return z < 0 ? 0 : z;
        }


        private static void RenderWorldObjectLegend(StringBuilder sb, WorldObjectOverlayInfo worldObjects, int x, int y)
        {
            int activeCount = worldObjects.Segment.Records.Count(record => !record.IsEmpty);
            sb.AppendLine($"  <text class=\"legend\" x=\"{x}\" y=\"{y}\">OOL/world-object overlay: {activeCount} active record(s) from {Escape(Path.GetFileName(worldObjects.File.SourcePath))} [{Escape(worldObjects.Segment.Name)}]</text>");
            sb.AppendLine($"  <circle cx=\"{x + 12}\" cy=\"{y + 10}\" r=\"5\" fill=\"#cc3f8d\" fill-opacity=\"0.82\" stroke=\"#111\" stroke-width=\"0.7\" />");
            sb.AppendLine($"  <text class=\"legend\" x=\"{x + 24}\" y=\"{y + 14}\">marker = active 8-byte record; x/y are used as map coordinates, tail bytes are still raw</text>");
        }

        private static void RenderWorldObjectOverlay(StringBuilder sb, MapPlane plane, int mapX, int mapY, int tileSize, Look2File? look2, WorldObjectOverlayInfo? worldObjects)
        {
            if (worldObjects is null)
            {
                return;
            }

            int markerRadius = Math.Max(2, tileSize);
            foreach (OolRecord record in worldObjects.Segment.Records.Where(record => !record.IsEmpty))
            {
                if (record.PositionX >= plane.Width || record.PositionY >= plane.Height)
                {
                    continue;
                }

                int centerX = mapX + (record.PositionX * tileSize) + (tileSize / 2);
                int centerY = mapY + (record.PositionY * tileSize) + (tileSize / 2);
                string title = BuildWorldObjectTitle(record, plane, look2);
                sb.AppendLine($"  <circle cx=\"{centerX}\" cy=\"{centerY}\" r=\"{markerRadius}\" fill=\"#cc3f8d\" fill-opacity=\"0.82\" stroke=\"#111\" stroke-width=\"0.7\"><title>{Escape(title)}</title></circle>");
            }
        }

        private static string RenderWorldObjectSummary(MapDatFile mapFile, MapSection section, WorldObjectOverlayInfo worldObjects, Look2File? look2)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Source: {mapFile.SourcePath}");
            sb.AppendLine($"Section: {section.Name}");
            sb.AppendLine($"Kind: {mapFile.Kind}");
            sb.AppendLine($"OOL source: {worldObjects.File.SourcePath}");
            sb.AppendLine($"OOL segment: {worldObjects.Segment.Name}");
            sb.AppendLine();
            sb.AppendLine("Active OOL Records:");

            MapPlane plane = section.Planes[0];
            IReadOnlyList<OolRecord> activeRecords = worldObjects.Segment.Records.Where(record => !record.IsEmpty).ToArray();
            if (activeRecords.Count == 0)
            {
                sb.AppendLine("  (none)");
            }
            else
            {
                foreach (OolRecord record in activeRecords)
                {
                    sb.AppendLine($"Slot {record.SlotIndex:D2} @ ({record.PositionX},{record.PositionY})");
                    sb.AppendLine($"  Raw tail: [{record.GetRawTailHex()}]");
                    sb.AppendLine($"  Raw words: 0x{record.RawWord23:X4}, 0x{record.RawWord45:X4}, 0x{record.RawWord67:X4}");
                    sb.AppendLine($"  Map tile: {BuildUnderlyingTileText(record, plane, look2)}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("Notes:");
            sb.AppendLine("  - The first two bytes are used here as x/y world-map coordinates because they fit the 256x256 BRIT/UNDER planes cleanly.");
            sb.AppendLine("  - The remaining 6 bytes are preserved as raw metadata until their gameplay meaning is confirmed.");
            return sb.ToString().TrimEnd();
        }

        private static string BuildWorldObjectTitle(OolRecord record, MapPlane plane, Look2File? look2)
        {
            return $"OOL slot {record.SlotIndex} @ ({record.PositionX},{record.PositionY}) • raw=[{record.GetRawTailHex()}] • words=[0x{record.RawWord23:X4} 0x{record.RawWord45:X4} 0x{record.RawWord67:X4}] • {BuildUnderlyingTileText(record, plane, look2)}";
        }

        private static string BuildUnderlyingTileText(OolRecord record, MapPlane plane, Look2File? look2)
        {
            if (record.PositionX >= plane.Width || record.PositionY >= plane.Height)
            {
                return "outside plane bounds";
            }

            int tileId = plane.Tiles[(record.PositionY * plane.Width) + record.PositionX];
            string description = look2?.GetDescription(tileId) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                return $"underlying tile 0x{tileId:X2}";
            }

            return $"underlying tile 0x{tileId:X2}: {description}";
        }

        private static string RenderNpcSummary(MapDatFile mapFile, MapSection section, IReadOnlyDictionary<int, NpcIdentityInfo> npcIdentities)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Source: {mapFile.SourcePath}");
            sb.AppendLine($"Section: {section.Name}");
            sb.AppendLine($"Kind: {mapFile.Kind}");
            sb.AppendLine($"Planes: {section.Planes.Count}");
            sb.AppendLine();
            sb.AppendLine("Active NPC Slots:");

            if (section.NpcMap is null)
            {
                sb.AppendLine("  (none)");
                return sb.ToString();
            }

            bool hasBasement = section.Planes.Count == 5;
            IReadOnlyDictionary<int, string> planeNames = section.Planes.ToDictionary(plane => plane.PlaneIndexWithinSection, plane => plane.LevelLabel);

            foreach (NpcSlot slot in section.NpcMap.Slots.Where(slot => slot.HasAnyData && slot.SlotIndex > 0))
            {
                NpcIdentityInfo? identity = ResolveNpcIdentity(slot, npcIdentities);
                string identityLine = identity is null ? string.Empty : $" • TLK {identity.NpcId}: {identity.DisplayName}";
                sb.AppendLine($"Slot {slot.SlotIndex:D2} • type {slot.Type} • dialog {slot.DialogNumber} ({slot.DialogMeaning}){identityLine}");
                sb.AppendLine($"  Time bytes: {string.Join(", ", slot.Schedule.Times.Select(time => $"0x{time:X2}"))}");
                foreach (NpcScheduleStop stop in slot.Schedule.Stops)
                {
                    int planeIndex = ResolveNpcPlaneIndex(stop.Z, hasBasement);
                    string planeLabel = planeNames.TryGetValue(planeIndex, out string? levelLabel)
                        ? levelLabel
                        : $"plane {planeIndex}";
                    sb.AppendLine($"  Stop {stop.StopIndex}: ai=0x{stop.AiType:X2} pos=({stop.X},{stop.Y},{stop.Z}) -> {planeLabel}");
                }

                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static string RenderManifest(MapDatFile mapFile, IReadOnlyList<MapRenderOutputFile> outputFiles, IReadOnlyList<string> warnings)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Source: {mapFile.SourcePath}");
            sb.AppendLine($"Kind: {mapFile.Kind}");
            sb.AppendLine($"Sections: {mapFile.Sections.Count}");
            sb.AppendLine();
            sb.AppendLine("Output Files:");
            foreach (MapRenderOutputFile file in outputFiles)
            {
                sb.AppendLine($"  {file.Kind,-12} {file.FullPath}  ({file.Description})");
            }

            if (warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (string warning in warnings)
                {
                    sb.AppendLine($"  {warning}");
                }
            }

            return sb.ToString();
        }

        private static NpcIdentityInfo? ResolveNpcIdentity(NpcSlot slot, IReadOnlyDictionary<int, NpcIdentityInfo> npcIdentities)
        {
            if (slot.DialogNumber >= 129)
            {
                return null;
            }

            return npcIdentities.TryGetValue(slot.DialogNumber, out NpcIdentityInfo? identity) ? identity : null;
        }

        private static string BuildNpcLabel(NpcSlot slot, NpcIdentityInfo? identity)
        {
            if (identity is null)
            {
                return $"{slot.SlotIndex:D2}";
            }

            string label = $"{slot.SlotIndex:D2} {identity.DisplayName}";
            return label.Length <= 18 ? label : label[..17] + "…";
        }

        private static IReadOnlyDictionary<int, NpcIdentityInfo> TryLoadNpcIdentities(string sourcePath)
        {
            string? tlkPath = TryLocateSiblingTlk(sourcePath);
            if (tlkPath is null || !File.Exists(tlkPath))
            {
                return new Dictionary<int, NpcIdentityInfo>();
            }

            try
            {
                TlkParser parser = new TlkParser();
                TlkFile tlkFile = parser.ParseFile(tlkPath);
                string? dataOvlPath = DataOvlInspector.TryLocateSiblingDataOvl(tlkPath);
                TlkDataOvlDictionary? dictionary = dataOvlPath is null || !File.Exists(dataOvlPath)
                    ? null
                    : TlkDataOvlDictionary.LoadFile(dataOvlPath);

                TlkAnalysisReport report = new TlkAnalysisService().Analyze(tlkFile, dictionary, dataOvlPath);
                Dictionary<int, NpcIdentityInfo> identities = new Dictionary<int, NpcIdentityInfo>();
                foreach (TlkNpcAnalysis analysis in report.NpcAnalyses)
                {
                    string? name = ExtractFixedFieldText(analysis, "Name");
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = $"TLK {analysis.NpcId}";
                    }

                    NpcIdentityInfo identity = new NpcIdentityInfo
                    {
                        NpcId = analysis.NpcId,
                        EntryIndex = analysis.EntryIndex,
                        DisplayName = name
                    };

                    identities[(int)analysis.NpcId] = identity;
                    if (!identities.ContainsKey(analysis.EntryIndex + 1))
                    {
                        identities[analysis.EntryIndex + 1] = identity;
                    }
                }

                return identities;
            }
            catch
            {
                return new Dictionary<int, NpcIdentityInfo>();
            }
        }

        private static string? TryLocateSiblingTlk(string sourcePath)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Directory.GetCurrentDirectory();
            string tlkPath = Path.Combine(directory, Path.GetFileNameWithoutExtension(sourcePath).ToUpperInvariant() + ".TLK");
            return File.Exists(tlkPath) ? tlkPath : null;
        }

        private static string? ExtractFixedFieldText(TlkNpcAnalysis analysis, string fieldName)
        {
            TlkFixedEntry? field = analysis.FixedEntries.FirstOrDefault(entry => string.Equals(entry.Name, fieldName, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                return null;
            }

            return SanitizeVisibleText(field.DecodedText);
        }

        private static string SanitizeVisibleText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string text = value;
            int controlIndex = text.IndexOf('<');
            if (controlIndex >= 0)
            {
                text = text[..controlIndex];
            }

            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            while (text.Contains("  ", StringComparison.Ordinal))
            {
                text = text.Replace("  ", " ", StringComparison.Ordinal);
            }

            return text.Trim();
        }

        private static string BuildSectionSubtitle(MapDatFile mapFile, MapSection section, bool hasTileGraphics, bool hasWorldObjects)
        {
            string tileInfo = hasTileGraphics ? "16-color tiles enabled" : "heuristic colors";
            return mapFile.Kind switch
            {
                MapDatKind.Britannia or MapDatKind.Underworld => $"{section.Planes[0].Width}x{section.Planes[0].Height} tile map • {tileInfo} • OOL overlay {(hasWorldObjects ? "enabled" : "not available")}",
                _ => $"{section.Planes.Count} floor(s) • {tileInfo} • NPC overlay {(section.NpcMap is null ? "not available" : "enabled")}"
            };
        }

        private static void RenderTileDefinitions(StringBuilder sb, Tile16File tileSet, IReadOnlyCollection<int> usedTileIds)
        {
            if (usedTileIds.Count == 0)
            {
                return;
            }

            IReadOnlyDictionary<byte, string> palette = tileSet.Palette.ToDictionary(entry => entry.Index, entry => entry.ToHexRgb());
            sb.AppendLine("  <defs>");
            foreach (int tileId in usedTileIds)
            {
                Tile16Tile tile = tileSet.Tiles[tileId];
                sb.AppendLine($"    <symbol id=\"tile-{tileId:X4}\" viewBox=\"0 0 {tile.Width} {tile.Height}\" overflow=\"visible\">");
                for (int y = 0; y < tile.Height; y++)
                {
                    int rowOffset = y * tile.Width;
                    int runStart = 0;
                    byte current = tile.Pixels[rowOffset];
                    for (int x = 1; x <= tile.Width; x++)
                    {
                        byte next = x < tile.Width ? tile.Pixels[rowOffset + x] : (byte)0xFF;
                        if (x < tile.Width && next == current)
                        {
                            continue;
                        }

                        string fill = palette.TryGetValue(current, out string? color) ? color : "#000000";
                        int width = x - runStart;
                        sb.AppendLine($"      <rect x=\"{runStart}\" y=\"{y}\" width=\"{width}\" height=\"1\" fill=\"{fill}\" />");
                        runStart = x;
                        current = next;
                    }
                }

                sb.AppendLine("    </symbol>");
            }
            sb.AppendLine("  </defs>");
        }

        private static string GetTileColor(int tileId, string description)
        {
            string text = description.ToLowerInvariant();
            if (tileId == 0x01 || text.Contains("water") || text.Contains("sea") || text.Contains("ocean"))
            {
                return "#2f6db3";
            }

            if (text.Contains("shallow") || text.Contains("ford"))
            {
                return "#62a7d9";
            }

            if (text.Contains("grass") || text.Contains("field") || text.Contains("pasture") || text.Contains("plains"))
            {
                return "#7aa95c";
            }

            if (text.Contains("brush") || text.Contains("bush") || text.Contains("swamp") || text.Contains("marsh"))
            {
                return "#6c7f39";
            }

            if (text.Contains("tree") || text.Contains("forest") || text.Contains("wood"))
            {
                return "#335c3c";
            }

            if (text.Contains("mount") || text.Contains("hill") || text.Contains("rock") || text.Contains("stone") || text.Contains("cliff"))
            {
                return "#7a7a7a";
            }

            if (text.Contains("lava") || text.Contains("fire"))
            {
                return "#cc5a2d";
            }

            if (text.Contains("wall") || text.Contains("door") || text.Contains("window") || text.Contains("castle") || text.Contains("brick") || text.Contains("floor"))
            {
                return "#b7b0a3";
            }

            if (text.Contains("roof"))
            {
                return "#8c4c3b";
            }

            if (text.Contains("road") || text.Contains("path") || text.Contains("bridge") || text.Contains("stairs") || text.Contains("ladder"))
            {
                return "#c2a878";
            }

            if (text.Contains("sign") || text.Contains("banner") || text.Contains("moongate") || text.Contains("altar") || text.Contains("shrine"))
            {
                return "#c7a93b";
            }

            if (text.Contains("field") && text.Contains("energy"))
            {
                return "#8a63d2";
            }

            return tileId switch
            {
                0x00 => "#1b4d8f",
                0x01 => "#2f6db3",
                0x04 or 0x05 => "#7aa95c",
                0x0A or 0x0B => "#7a7a7a",
                _ => "#9a8f80"
            };
        }

        private static string Slugify(string value)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char ch in value)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else if (sb.Length == 0 || sb[^1] != '-')
                {
                    sb.Append('-');
                }
            }

            return sb.ToString().Trim('-');
        }

        private static string Escape(string value)
        {
            return SecurityElement.Escape(value) ?? string.Empty;
        }


        private sealed class WorldObjectOverlayInfo
        {
            public required OolFile File { get; init; }

            public required OolSegment Segment { get; init; }
        }

        private sealed class NpcIdentityInfo
        {
            public required int NpcId { get; init; }

            public required int EntryIndex { get; init; }

            public required string DisplayName { get; init; }
        }
    }
}

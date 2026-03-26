namespace U5.Core.Formats.Ool
{
    public sealed class OolParser
    {
        public const int RecordSize = 8;
        public const int StandardSegmentLength = 0x100;
        public const int StandardRecordCountPerSegment = StandardSegmentLength / RecordSize;

        public OolFile ParseFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("OOL file was not found.", path);
            }

            return Parse(fileInfo.FullName, File.ReadAllBytes(fileInfo.FullName));
        }

        public OolFile Parse(string sourcePath, byte[] bytes)
        {
            string baseName = Path.GetFileNameWithoutExtension(sourcePath).ToUpperInvariant();
            OolFileKind kind = DetectKind(baseName);
            List<string> warnings = new List<string>();
            List<OolSegment> segments = new List<OolSegment>();

            if (bytes.Length % RecordSize != 0)
            {
                throw new InvalidDataException($"OOL length {bytes.Length} is not a multiple of the {RecordSize}-byte record size.");
            }

            if (bytes.Length == StandardSegmentLength)
            {
                segments.Add(ParseSegment(bytes, 0, ResolveSingleSegmentName(kind, baseName)));
            }
            else if (bytes.Length == StandardSegmentLength * 2)
            {
                if (kind != OolFileKind.Saved)
                {
                    warnings.Add($"{baseName}.OOL has a nonstandard 0x200-byte layout; treating it as two 0x100-byte world segments.");
                }

                segments.Add(ParseSegment(bytes[..StandardSegmentLength], 0, "Britannia"));
                segments.Add(ParseSegment(bytes[StandardSegmentLength..], 1, "Underworld"));
            }
            else if (bytes.Length % StandardSegmentLength == 0)
            {
                int segmentCount = bytes.Length / StandardSegmentLength;
                warnings.Add($"OOL length {bytes.Length} is larger than the known 0x100/0x200 layouts; exposing {segmentCount} generic 0x100-byte segment(s).\n");
                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    byte[] segmentBytes = bytes.Skip(segmentIndex * StandardSegmentLength).Take(StandardSegmentLength).ToArray();
                    segments.Add(ParseSegment(segmentBytes, segmentIndex, $"Segment {segmentIndex}"));
                }
            }
            else
            {
                warnings.Add($"OOL length {bytes.Length} uses a nonstandard record count; exposing it as a single raw segment.");
                segments.Add(ParseVariableLengthSegment(bytes, 0, ResolveSingleSegmentName(kind, baseName)));
            }

            return new OolFile
            {
                SourcePath = sourcePath,
                BaseName = baseName,
                Kind = kind,
                FileSize = bytes.Length,
                Segments = segments,
                Warnings = warnings
            };
        }

        public static string? TryLocateSiblingOol(string sourcePath)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(sourcePath)) ?? Directory.GetCurrentDirectory();
            string baseName = Path.GetFileNameWithoutExtension(sourcePath).ToUpperInvariant();
            List<string> candidates = new List<string>
            {
                Path.Combine(directory, baseName + ".OOL"),
                Path.Combine(directory, baseName.ToLowerInvariant() + ".ool")
            };

            if (baseName == "UNDER")
            {
                candidates.Add(Path.Combine(directory, "INIT.OOL"));
                candidates.Add(Path.Combine(directory, "init.ool"));
            }

            return candidates.FirstOrDefault(File.Exists);
        }

        private static OolSegment ParseSegment(byte[] bytes, int segmentIndex, string name)
        {
            List<OolRecord> records = new List<OolRecord>(StandardRecordCountPerSegment);
            for (int offset = 0; offset < bytes.Length; offset += RecordSize)
            {
                records.Add(ParseRecord(bytes, offset, offset / RecordSize));
            }

            return new OolSegment
            {
                SegmentIndex = segmentIndex,
                Name = name,
                Records = records
            };
        }

        private static OolSegment ParseVariableLengthSegment(byte[] bytes, int segmentIndex, string name)
        {
            List<OolRecord> records = new List<OolRecord>(bytes.Length / RecordSize);
            for (int offset = 0; offset < bytes.Length; offset += RecordSize)
            {
                records.Add(ParseRecord(bytes, offset, offset / RecordSize));
            }

            return new OolSegment
            {
                SegmentIndex = segmentIndex,
                Name = name,
                Records = records
            };
        }

        private static OolRecord ParseRecord(byte[] bytes, int offset, int slotIndex)
        {
            return new OolRecord
            {
                SlotIndex = slotIndex,
                PositionX = bytes[offset + 0],
                PositionY = bytes[offset + 1],
                RawByte2 = bytes[offset + 2],
                RawByte3 = bytes[offset + 3],
                RawByte4 = bytes[offset + 4],
                RawByte5 = bytes[offset + 5],
                RawByte6 = bytes[offset + 6],
                RawByte7 = bytes[offset + 7]
            };
        }

        private static OolFileKind DetectKind(string baseName)
        {
            return baseName switch
            {
                "BRIT" => OolFileKind.Britannia,
                "UNDER" => OolFileKind.Underworld,
                "INIT" => OolFileKind.Initial,
                "SAVED" => OolFileKind.Saved,
                _ => OolFileKind.Unknown
            };
        }

        private static string ResolveSingleSegmentName(OolFileKind kind, string baseName)
        {
            return kind switch
            {
                OolFileKind.Britannia => "Britannia",
                OolFileKind.Underworld => "Underworld",
                OolFileKind.Initial => "Initial / Underworld",
                _ => baseName
            };
        }
    }
}

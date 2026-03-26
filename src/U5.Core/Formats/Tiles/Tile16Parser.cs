using U5.Core.Compression;

namespace U5.Core.Formats.Tiles
{
    public sealed class Tile16Parser
    {
        public const int TileCount = 0x200;
        public const int TileWidth = 16;
        public const int TileHeight = 16;
        public const int BytesPerRow = 8;
        public const int BytesPerTile = TileHeight * BytesPerRow;
        public const int ExpectedFileLength = TileCount * BytesPerTile;

        private static readonly Tile16PaletteEntry[] EgaPalette = new Tile16PaletteEntry[]
        {
            new Tile16PaletteEntry { Index = 0x00, Red = 0, Green = 0, Blue = 0 },
            new Tile16PaletteEntry { Index = 0x01, Red = 0, Green = 0, Blue = 170 },
            new Tile16PaletteEntry { Index = 0x02, Red = 0, Green = 170, Blue = 0 },
            new Tile16PaletteEntry { Index = 0x03, Red = 0, Green = 170, Blue = 170 },
            new Tile16PaletteEntry { Index = 0x04, Red = 170, Green = 0, Blue = 0 },
            new Tile16PaletteEntry { Index = 0x05, Red = 170, Green = 0, Blue = 170 },
            new Tile16PaletteEntry { Index = 0x06, Red = 170, Green = 85, Blue = 0 },
            new Tile16PaletteEntry { Index = 0x07, Red = 170, Green = 170, Blue = 170 },
            new Tile16PaletteEntry { Index = 0x08, Red = 85, Green = 85, Blue = 85 },
            new Tile16PaletteEntry { Index = 0x09, Red = 85, Green = 85, Blue = 255 },
            new Tile16PaletteEntry { Index = 0x0A, Red = 85, Green = 255, Blue = 85 },
            new Tile16PaletteEntry { Index = 0x0B, Red = 85, Green = 255, Blue = 255 },
            new Tile16PaletteEntry { Index = 0x0C, Red = 255, Green = 85, Blue = 85 },
            new Tile16PaletteEntry { Index = 0x0D, Red = 255, Green = 85, Blue = 255 },
            new Tile16PaletteEntry { Index = 0x0E, Red = 255, Green = 255, Blue = 85 },
            new Tile16PaletteEntry { Index = 0x0F, Red = 255, Green = 255, Blue = 255 }
        };

        public Tile16File ParseFile(string path)
        {
            return Parse(path, File.ReadAllBytes(path));
        }


        public byte[] ExpandIfCompressed(byte[] bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length >= ExpectedFileLength)
            {
                return bytes;
            }

            if (bytes.Length < 5)
            {
                return bytes;
            }

            int uncompressedLength = BitConverter.ToInt32(bytes, 0);
            if (uncompressedLength <= 0)
            {
                return bytes;
            }

            if (bytes.Length >= uncompressedLength)
            {
                return bytes;
            }

            byte[] compressedBytes = new byte[bytes.Length - 4];
            Buffer.BlockCopy(bytes, 4, compressedBytes, 0, compressedBytes.Length);
            return U5Lzw.Decompress(compressedBytes, uncompressedLength);
        }

        public Tile16File Parse(string sourcePath, byte[] bytes)
        {
            byte[] expandedBytes = ExpandIfCompressed(bytes);
            if (expandedBytes.Length < ExpectedFileLength)
            {
                throw new InvalidDataException($"TILES.16 raw tile data is expected to be at least {ExpectedFileLength} bytes, but was {expandedBytes.Length}.");
            }

            List<Tile16Tile> tiles = new List<Tile16Tile>(TileCount);
            for (int tileIndex = 0; tileIndex < TileCount; tileIndex++)
            {
                int tileOffset = tileIndex * BytesPerTile;
                byte[] pixels = new byte[TileWidth * TileHeight];
                int pixelIndex = 0;
                for (int row = 0; row < TileHeight; row++)
                {
                    int rowOffset = tileOffset + (row * BytesPerRow);
                    for (int columnByte = 0; columnByte < BytesPerRow; columnByte++)
                    {
                        byte packed = expandedBytes[rowOffset + columnByte];
                        pixels[pixelIndex++] = (byte)((packed >> 4) & 0x0F);
                        pixels[pixelIndex++] = (byte)(packed & 0x0F);
                    }
                }

                tiles.Add(new Tile16Tile
                {
                    TileId = tileIndex,
                    Width = TileWidth,
                    Height = TileHeight,
                    Pixels = pixels
                });
            }

            return new Tile16File
            {
                SourcePath = sourcePath,
                Tiles = tiles,
                Palette = EgaPalette
            };
        }
    }
}

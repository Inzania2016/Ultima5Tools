namespace U5.Core.Formats.Dat
{
    public sealed class Look2Parser
    {
        public Look2File ParseFile(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("LOOK2.DAT file was not found.", path);
            }

            return Parse(File.ReadAllBytes(path));
        }

        public Look2File Parse(byte[] bytes)
        {
            if (bytes.Length < Look2File.TileCount * 2)
            {
                throw new InvalidDataException("LOOK2.DAT is too small to contain the description index table.");
            }

            List<string> descriptions = new List<string>(Look2File.TileCount);
            for (int tileId = 0; tileId < Look2File.TileCount; tileId++)
            {
                int offset = BitConverter.ToUInt16(bytes, tileId * 2);
                descriptions.Add(ReadAsciiZ(bytes, offset));
            }

            return new Look2File
            {
                Descriptions = descriptions
            };
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
    }
}

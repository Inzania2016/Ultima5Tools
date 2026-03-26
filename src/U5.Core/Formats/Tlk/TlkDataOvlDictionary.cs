namespace U5.Core.Formats.Tlk
{
    public sealed class TlkDataOvlDictionary
    {
        public const int TokenCount = 129;
        public const int PointerTableOffset = 0x24F8;
        public const int PointerBias = 0x10;

        private readonly IReadOnlyList<string> _tokens;

        private TlkDataOvlDictionary(IReadOnlyList<string> tokens)
        {
            _tokens = tokens;
        }

        public string Resolve(byte token)
        {
            return token < _tokens.Count ? _tokens[token] : string.Empty;
        }

        public static TlkDataOvlDictionary LoadFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Load(bytes);
        }

        public static TlkDataOvlDictionary Load(byte[] bytes)
        {
            if (PointerTableOffset + (TokenCount * 2) > bytes.Length)
            {
                throw new InvalidDataException("DATA.OVL is too small to contain the TLK token table.");
            }

            List<string> tokens = new List<string>(TokenCount);
            for (int i = 0; i < TokenCount; i++)
            {
                int pointerOffset = PointerTableOffset + (i * 2);
                ushort pointer = BitConverter.ToUInt16(bytes, pointerOffset);

                if (pointer == 0)
                {
                    tokens.Add(string.Empty);
                    continue;
                }

                int textOffset = pointer + PointerBias;
                if (textOffset < 0 || textOffset >= bytes.Length)
                {
                    tokens.Add($"<BAD_TOKEN_PTR_{pointer:X4}>");
                    continue;
                }

                int terminator = Array.IndexOf(bytes, (byte)0x00, textOffset);
                if (terminator < 0)
                {
                    terminator = bytes.Length;
                }

                int length = terminator - textOffset;
                string text = length <= 0
                    ? string.Empty
                    : System.Text.Encoding.Latin1.GetString(bytes, textOffset, length);

                tokens.Add(text);
            }

            return new TlkDataOvlDictionary(tokens);
        }

        public static string? TryLocateSiblingDataOvl(string tlkPath)
        {
            string? directory = Path.GetDirectoryName(tlkPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return null;
            }

            string[] candidates = new string[]
            {
                Path.Combine(directory, "DATA.OVL"),
                Path.Combine(directory, "data.ovl")
            };

            return candidates.FirstOrDefault(File.Exists);
        }
    }
}

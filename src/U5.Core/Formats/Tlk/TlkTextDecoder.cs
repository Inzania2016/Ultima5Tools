using System.Text;

namespace U5.Core.Formats.Tlk
{
    public sealed class TlkTextDecoder
    {
        private readonly TlkDataOvlDictionary? _dictionary;

        public TlkTextDecoder(TlkDataOvlDictionary? dictionary)
        {
            _dictionary = dictionary;
        }

        public TlkDecodedString DecodeZeroTerminated(byte[] bytes, int offset)
        {
            if (offset < 0 || offset >= bytes.Length)
            {
                return new TlkDecodedString
                {
                    DecodedText = string.Empty,
                    BytesConsumed = 0,
                    RawBytes = Array.Empty<byte>()
                };
            }

            StringBuilder builder = new StringBuilder();
            int position = offset;

            while (position < bytes.Length)
            {
                byte value = bytes[position];
                if (value == 0x00)
                {
                    position++;
                    break;
                }

                TlkDecodedAtom currentAtom = Translate(value);

                if (currentAtom.Kind == TlkDecodedAtomKind.Dictionary)
                {
                    if (NeedsLeadingSpace(builder, currentAtom.Text))
                    {
                        builder.Append(' ');
                    }

                    builder.Append(currentAtom.Text);

                    if (position + 1 < bytes.Length && bytes[position + 1] != 0x00)
                    {
                        TlkDecodedAtom nextAtom = Translate(bytes[position + 1]);
                        if (NeedsTrailingSpace(currentAtom.Text, nextAtom.Text))
                        {
                            builder.Append(' ');
                        }
                    }
                }
                else
                {
                    builder.Append(currentAtom.Text);
                }

                position++;
            }

            int consumed = position - offset;
            byte[] rawBytes = bytes.Skip(offset).Take(consumed).ToArray();

            return new TlkDecodedString
            {
                DecodedText = builder.ToString(),
                BytesConsumed = consumed,
                RawBytes = rawBytes
            };
        }

        private static bool NeedsLeadingSpace(StringBuilder builder, string nextText)
        {
            if (builder.Length == 0 || string.IsNullOrEmpty(nextText))
            {
                return false;
            }

            char previous = builder[builder.Length - 1];
            return (char.IsLetterOrDigit(previous) || previous == ',' || previous == ';' || previous == ':')
                && char.IsLetterOrDigit(nextText[0]);
        }

        private static bool NeedsTrailingSpace(string currentText, string nextText)
        {
            if (string.IsNullOrEmpty(currentText) || string.IsNullOrEmpty(nextText))
            {
                return false;
            }

            return char.IsLetterOrDigit(currentText[currentText.Length - 1])
                && char.IsLetterOrDigit(nextText[0]);
        }

        private TlkDecodedAtom Translate(byte value)
        {
            if (value == 0xA2)
            {
                return new TlkDecodedAtom("<QUOTE>", TlkDecodedAtomKind.Control);
            }

            if (value >= 0xA0 && value < 0xFF)
            {
                return new TlkDecodedAtom(((char)(value - 0x80)).ToString(), TlkDecodedAtomKind.Ascii);
            }

            if (value < 0x81)
            {
                string tokenText = _dictionary is null
                    ? $"<TOK_{value:X2}>"
                    : _dictionary.Resolve(value);

                if (string.IsNullOrEmpty(tokenText))
                {
                    tokenText = $"<TOK_{value:X2}>";
                }

                return new TlkDecodedAtom(tokenText, TlkDecodedAtomKind.Dictionary);
            }

            return new TlkDecodedAtom(MapControlCode(value), TlkDecodedAtomKind.Control);
        }

        private static string MapControlCode(byte value)
        {
            return value switch
            {
                0x81 => "<AVATAR_NAME>",
                0x82 => "<END_DIALOG>",
                0x83 => "<PAUSE>",
                0x84 => "<JOIN_PARTY>",
                0x85 => "<TAKE_GOLD>",
                0x86 => "<GIVE_ITEM>",
                0x87 => "<OR>",
                0x88 => "<ASK_NAME>",
                0x89 => "<KARMA_GAIN>",
                0x8A => "<KARMA_LOSS?>",
                0x8B => "<CALL_GUARDS>",
                0x8C => "<IF_KNOWS_NAME>",
                0x8D => "<NEWLINE>",
                0x8E => "<MAGIC_WORD>",
                0x8F => "<KEY_WAIT>",
                0x90 => "<NODE>",
                >= 0x91 and <= 0x9F => value == 0x9F ? "<END_SENTINEL>" : $"<LABEL_{value - 0x90}>",
                0xFE => "<CHECK_TOKEN>",
                0xFF => "<NO_OP>",
                _ => $"<0x{value:X2}>"
            };
        }

        private readonly struct TlkDecodedAtom
        {
            public TlkDecodedAtom(string text, TlkDecodedAtomKind kind)
            {
                Text = text;
                Kind = kind;
            }

            public string Text { get; }

            public TlkDecodedAtomKind Kind { get; }
        }

        private enum TlkDecodedAtomKind
        {
            Ascii,
            Dictionary,
            Control
        }
    }
}

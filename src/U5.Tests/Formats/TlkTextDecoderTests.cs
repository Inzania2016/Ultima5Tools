using U5.Core.Formats.Tlk;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class TlkTextDecoderTests
    {
        [Fact]
        public void DecodeZeroTerminated_DecodesAsciiDictionaryAndControls()
        {
            byte[] dictionaryBytes = new byte[TlkDataOvlDictionary.PointerTableOffset + (TlkDataOvlDictionary.TokenCount * 2) + 0x200];
            WriteToken(dictionaryBytes, 0x01, "the");
            WriteToken(dictionaryBytes, 0x09, "in");
            TlkDataOvlDictionary dictionary = TlkDataOvlDictionary.Load(dictionaryBytes);

            byte[] text = new byte[]
            {
                0xC9, 0xA0, 0xF3, 0xF4, 0xF5, 0xE4, 0xF9, 0x01, 0xF3, 0xF4, 0xE1, 0xF2, 0xF3, 0xAE, 0x00
            };

            TlkTextDecoder decoder = new TlkTextDecoder(dictionary);
            TlkDecodedString decoded = decoder.DecodeZeroTerminated(text, 0);

            Assert.Equal("I study the stars.", decoded.DecodedText);
            Assert.Equal(text.Length, decoded.BytesConsumed);
        }

        [Fact]
        public void DecodeZeroTerminated_RendersQuoteGlyphAsPlaceholder()
        {
            byte[] text = new byte[]
            {
                0xC8, 0xE9, 0xA2, 0x00
            };

            TlkTextDecoder decoder = new TlkTextDecoder(null);
            TlkDecodedString decoded = decoder.DecodeZeroTerminated(text, 0);

            Assert.Equal("Hi<QUOTE>", decoded.DecodedText);
        }

        private static void WriteToken(byte[] bytes, byte tokenIndex, string text)
        {
            int textOffset = TlkDataOvlDictionary.PointerTableOffset + 0x100 + (tokenIndex * 0x10);
            ushort storedPointer = (ushort)(textOffset - TlkDataOvlDictionary.PointerBias);

            bytes[TlkDataOvlDictionary.PointerTableOffset + (tokenIndex * 2)] = (byte)(storedPointer & 0xFF);
            bytes[TlkDataOvlDictionary.PointerTableOffset + (tokenIndex * 2) + 1] = (byte)(storedPointer >> 8);

            byte[] encoded = System.Text.Encoding.Latin1.GetBytes(text);
            Array.Copy(encoded, 0, bytes, textOffset, encoded.Length);
            bytes[textOffset + encoded.Length] = 0x00;
        }
    }
}

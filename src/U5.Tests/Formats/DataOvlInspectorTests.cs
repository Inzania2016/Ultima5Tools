using U5.Core.Formats.Npc;
using U5.Core.Formats.Ovl;
using U5.Core.Formats.Tlk;
using Xunit;

namespace U5.Tests.Formats
{
    public sealed class DataOvlInspectorTests
    {
        [Fact]
        public void Inspect_ExtractsTokenAndMapTables()
        {
            byte[] bytes = new byte[0x3000];
            WriteToken(bytes, 0x01, "the");
            bytes[0x1E2A] = 0;
            bytes[0x1E2A + 1] = 2;
            bytes[0x1E32] = 3;
            bytes[0x1E3A] = 1;
            bytes[0x1E42] = 4;

            WriteIndexedName(bytes, 0x1E4A, 0, 0x2800, "MOONGLOW");
            WriteIndexedName(bytes, 0x1E4A, 1, 0x2810, "BRITAIN");
            WriteIndexedName(bytes, 0x1E6E, 0, 0x2900, "WEST BRITANNY");

            DataOvlInspector inspector = new DataOvlInspector();
            DataOvlInfo info = inspector.Inspect(bytes);

            Assert.Equal("the", info.TlkTokens[0x01]);
            Assert.Equal("MOONGLOW", info.CityNames[0]);
            Assert.Equal("WEST BRITANNY", info.OtherLocationNames[0]);
            Assert.Equal((byte)0, info.GetMapSet(NpcFileKind.Towne)!.StartMapIndices[0]);
            Assert.Equal((byte)4, info.GetMapSet(NpcFileKind.Keep)!.StartMapIndices[0]);
        }

        private static void WriteToken(byte[] bytes, byte tokenIndex, string text)
        {
            int textOffset = 0x2A00 + (tokenIndex * 0x10);
            ushort storedPointer = (ushort)(textOffset - TlkDataOvlDictionary.PointerBias);
            bytes[TlkDataOvlDictionary.PointerTableOffset + (tokenIndex * 2)] = (byte)(storedPointer & 0xFF);
            bytes[TlkDataOvlDictionary.PointerTableOffset + (tokenIndex * 2) + 1] = (byte)(storedPointer >> 8);

            byte[] encoded = System.Text.Encoding.Latin1.GetBytes(text);
            Array.Copy(encoded, 0, bytes, textOffset, encoded.Length);
            bytes[textOffset + encoded.Length] = 0x00;
        }

        private static void WriteIndexedName(byte[] bytes, int tableOffset, int entryIndex, int textOffset, string text)
        {
            ushort storedPointer = (ushort)(textOffset - 0x10);
            bytes[tableOffset + (entryIndex * 2)] = (byte)(storedPointer & 0xFF);
            bytes[tableOffset + (entryIndex * 2) + 1] = (byte)(storedPointer >> 8);

            byte[] encoded = System.Text.Encoding.Latin1.GetBytes(text);
            Array.Copy(encoded, 0, bytes, textOffset, encoded.Length);
            bytes[textOffset + encoded.Length] = 0x00;
        }
    }
}

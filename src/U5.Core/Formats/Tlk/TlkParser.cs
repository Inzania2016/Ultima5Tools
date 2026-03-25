using U5.Core.IO;

namespace U5.Core.Formats.Tlk
{
    public sealed class TlkParser
    {
        public TlkFile ParseFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Parse(bytes);
        }

        public TlkFile Parse(byte[] bytes)
        {
            LittleEndianDataReader reader = new LittleEndianDataReader(bytes);
            ushort npcCount = reader.ReadUInt16(0);

            int tableOffset = 2;
            int tableEntrySize = 4;
            int tableByteCount = npcCount * tableEntrySize;
            if (tableOffset + tableByteCount > bytes.Length)
            {
                throw new InvalidDataException("TLK table exceeds file length.");
            }

            List<TlkEntry> entries = new List<TlkEntry>(npcCount);
            for (int i = 0; i < npcCount; i++)
            {
                int entryOffset = tableOffset + (i * tableEntrySize);
                ushort npcId = reader.ReadUInt16(entryOffset);
                ushort blockOffset = reader.ReadUInt16(entryOffset + 2);
                entries.Add(new TlkEntry
                {
                    NpcId = npcId,
                    BlockOffset = blockOffset
                });
            }

            int eof = bytes.Length;
            for (int i = 0; i < entries.Count; i++)
            {
                int current = entries[i].BlockOffset;
                int next = i + 1 < entries.Count ? entries[i + 1].BlockOffset : eof;

                if (next >= current && current <= eof)
                {
                    entries[i].InferredBlockSize = next - current;
                }
                else
                {
                    entries[i].InferredBlockSize = null;
                }
            }

            return new TlkFile
            {
                NpcCount = npcCount,
                Entries = entries,
                RawBytes = bytes
            };
        }
    }
}

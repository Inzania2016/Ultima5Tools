using U5.Core.IO;

namespace U5.Core.Formats.Npc
{
    public sealed class NpcParser
    {
        public NpcFile ParseFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Parse(bytes);
        }

        public NpcFile Parse(byte[] bytes)
        {
            LittleEndianDataReader reader = new LittleEndianDataReader(bytes);
            int expectedLength = NpcFile.ExpectedRecordCount * NpcFile.RecordSize;
            bool isExpectedLength = bytes.Length == expectedLength;

            int recordCount = bytes.Length / NpcFile.RecordSize;
            List<NpcRecord> records = new List<NpcRecord>(recordCount);

            for (int i = 0; i < recordCount; i++)
            {
                int offset = i * NpcFile.RecordSize;
                byte[] payload = reader.ReadBytes(offset, NpcFile.RecordSize);
                records.Add(new NpcRecord
                {
                    Index = i,
                    RawBytes = payload
                });
            }

            return new NpcFile
            {
                Records = records,
                IsExpectedLength = isExpectedLength,
                RawBytes = bytes
            };
        }
    }
}

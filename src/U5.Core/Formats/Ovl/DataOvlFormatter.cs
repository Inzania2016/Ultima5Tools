namespace U5.Core.Formats.Ovl
{
    public static class DataOvlFormatter
    {
        public static string Format(DataOvlInfo info)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine("DATA.OVL Analysis");
            writer.WriteLine($"File Size: {info.FileSize} bytes");
            writer.WriteLine($"TLK Token Count: {info.TlkTokens.Count}");
            writer.WriteLine();

            writer.WriteLine("TLK Compressed Words:");
            for (int i = 0; i < info.TlkTokens.Count; i++)
            {
                writer.WriteLine($"  [{i:X2}] {info.TlkTokens[i]}");
            }

            writer.WriteLine();
            writer.WriteLine("Map Start Index Sets:");
            foreach (DataOvlMapSetInfo mapSet in info.MapSets)
            {
                writer.WriteLine($"  {mapSet.DatName}:");
                for (int i = 0; i < mapSet.StartMapIndices.Count; i++)
                {
                    string sectionName = i < mapSet.SectionNames.Count ? mapSet.SectionNames[i] : $"Section {i}";
                    writer.WriteLine($"    [{i}] {sectionName} -> {mapSet.StartMapIndices[i]}");
                }
            }

            writer.WriteLine();
            writer.WriteLine("City Name Index Table:");
            for (int i = 0; i < info.CityNames.Count; i++)
            {
                writer.WriteLine($"  [{i:D2}] {info.CityNames[i]}");
            }

            writer.WriteLine();
            writer.WriteLine("Other Location Name Index Table:");
            for (int i = 0; i < info.OtherLocationNames.Count; i++)
            {
                writer.WriteLine($"  [{i:D2}] {info.OtherLocationNames[i]}");
            }

            return writer.ToString();
        }
    }
}

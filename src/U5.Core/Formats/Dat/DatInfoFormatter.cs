using U5.Core.Utilities;

namespace U5.Core.Formats.Dat
{
    public static class DatInfoFormatter
    {
        public static string Format(DatFileInfo info)
        {
            StringWriter writer = new StringWriter();
            writer.WriteLine($"Path: {info.FullPath}");
            writer.WriteLine($"Basename: {info.BaseName}");
            writer.WriteLine($"Extension: {info.Extension}");
            writer.WriteLine($"Size: {info.FileSize} bytes ({FileSizeFormatting.ToHumanReadable(info.FileSize)})");
            writer.WriteLine($"Note: {info.HeuristicNote}");
            return writer.ToString();
        }
    }
}

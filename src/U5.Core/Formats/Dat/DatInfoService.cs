namespace U5.Core.Formats.Dat
{
    public sealed class DatInfoService
    {
        public DatFileInfo Inspect(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("DAT file was not found.", path);
            }

            string extension = fileInfo.Extension;
            string note = extension.Equals(".dat", StringComparison.OrdinalIgnoreCase)
                ? "DAT extension detected. Format family is mixed and currently treated as opaque bytes."
                : "Non-DAT extension. Metadata inspection still applies.";

            return new DatFileInfo
            {
                FullPath = fileInfo.FullName,
                BaseName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                Extension = extension,
                FileSize = fileInfo.Length,
                HeuristicNote = note
            };
        }
    }
}

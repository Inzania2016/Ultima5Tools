namespace U5.Core.Formats.Gam
{
    public sealed class GamParser
    {
        public GamFile ParseFile(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return new GamFile
            {
                SourceName = Path.GetFileName(path),
                Length = bytes.Length,
                RawBytes = bytes
            };
        }
    }
}

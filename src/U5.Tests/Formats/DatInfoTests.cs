using U5.Core.Formats.Dat;

namespace U5.Tests.Formats
{
    public sealed class DatInfoTests
    {
        [Fact]
        public void Inspect_ExtractsMetadata()
        {
            string path = Path.Combine(Path.GetTempPath(), $"u5-{Guid.NewGuid():N}.dat");
            try
            {
                File.WriteAllBytes(path, new byte[] { 0x10, 0x20, 0x30 });

                DatInfoService service = new DatInfoService();
                DatFileInfo info = service.Inspect(path);

                Assert.Equal(".dat", info.Extension, ignoreCase: true);
                Assert.Equal(3, info.FileSize);
                Assert.Contains("mixed", info.HeuristicNote, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
    }
}

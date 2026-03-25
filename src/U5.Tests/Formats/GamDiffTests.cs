using U5.Core.Formats.Gam;

namespace U5.Tests.Formats
{
    public sealed class GamDiffTests
    {
        [Fact]
        public void Diff_DetectsContiguousChangedRanges()
        {
            GamFile left = new GamFile
            {
                SourceName = "left.gam",
                Length = 6,
                RawBytes = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 }
            };

            GamFile right = new GamFile
            {
                SourceName = "right.gam",
                Length = 6,
                RawBytes = new byte[] { 0x00, 0xFF, 0xFE, 0x03, 0x04, 0xAA }
            };

            GamDiffService service = new GamDiffService();
            GamDiffResult result = service.Diff(left, right);

            Assert.Equal(2, result.ChangedRanges.Count);
            Assert.Equal(1, result.ChangedRanges[0].StartOffset);
            Assert.Equal(2, result.ChangedRanges[0].Length);
            Assert.Equal(5, result.ChangedRanges[1].StartOffset);
            Assert.Single(result.ChangedRanges[1].RightBytes);
        }
    }
}

namespace U5.Core.Formats.Gam
{
    public sealed class GamDiffService
    {
        public GamDiffResult Diff(GamFile left, GamFile right)
        {
            int maxLength = Math.Max(left.Length, right.Length);
            List<GamDiffRange> ranges = new List<GamDiffRange>();

            int? rangeStart = null;
            for (int offset = 0; offset < maxLength; offset++)
            {
                byte leftByte = offset < left.Length ? left.RawBytes[offset] : (byte)0x00;
                byte rightByte = offset < right.Length ? right.RawBytes[offset] : (byte)0x00;

                bool isChanged = offset >= left.Length || offset >= right.Length || leftByte != rightByte;
                if (isChanged)
                {
                    if (!rangeStart.HasValue)
                    {
                        rangeStart = offset;
                    }
                }
                else if (rangeStart.HasValue)
                {
                    ranges.Add(CreateRange(left, right, rangeStart.Value, offset));
                    rangeStart = null;
                }
            }

            if (rangeStart.HasValue)
            {
                ranges.Add(CreateRange(left, right, rangeStart.Value, maxLength));
            }

            return new GamDiffResult
            {
                LeftLength = left.Length,
                RightLength = right.Length,
                ChangedRanges = ranges
            };
        }

        private static GamDiffRange CreateRange(GamFile left, GamFile right, int startOffset, int endOffset)
        {
            int length = endOffset - startOffset;
            byte[] leftBytes = ExtractPaddedRange(left.RawBytes, startOffset, length);
            byte[] rightBytes = ExtractPaddedRange(right.RawBytes, startOffset, length);

            return new GamDiffRange
            {
                StartOffset = startOffset,
                Length = length,
                LeftBytes = leftBytes,
                RightBytes = rightBytes
            };
        }

        private static byte[] ExtractPaddedRange(byte[] source, int startOffset, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
            {
                int sourceOffset = startOffset + i;
                result[i] = sourceOffset < source.Length ? source[sourceOffset] : (byte)0x00;
            }

            return result;
        }
    }
}

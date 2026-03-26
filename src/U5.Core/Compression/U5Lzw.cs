namespace U5.Core.Compression
{
    public static class U5Lzw
    {
        public static byte[] Decompress(byte[] compressedBytes, int expectedOutputLength)
        {
            if (compressedBytes is null)
            {
                throw new ArgumentNullException(nameof(compressedBytes));
            }

            if (expectedOutputLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedOutputLength));
            }

            const int clearCode = 256;
            const int endCode = 257;
            const int firstFreeCode = 258;
            const int maxCodeSize = 12;

            List<byte[]> dictionary = CreateInitialDictionary();
            int nextCode = firstFreeCode;
            int codeSize = 9;
            int bitOffset = 0;
            byte[]? previousEntry = null;
            List<byte> output = new List<byte>(expectedOutputLength > 0 ? expectedOutputLength : 4096);

            while (TryReadCode(compressedBytes, ref bitOffset, codeSize, out int code))
            {
                if (code == clearCode)
                {
                    dictionary = CreateInitialDictionary();
                    nextCode = firstFreeCode;
                    codeSize = 9;
                    previousEntry = null;
                    continue;
                }

                if (code == endCode)
                {
                    break;
                }

                byte[] entry;
                if (code < dictionary.Count && dictionary[code] is not null)
                {
                    entry = dictionary[code];
                }
                else if (code == nextCode && previousEntry is not null)
                {
                    entry = Concatenate(previousEntry, previousEntry[0]);
                }
                else
                {
                    throw new InvalidDataException($"Invalid LZW code {code} at bit offset {bitOffset - codeSize}.");
                }

                output.AddRange(entry);

                if (previousEntry is not null && dictionary.Count < 4096)
                {
                    dictionary.Add(Concatenate(previousEntry, entry[0]));
                    nextCode++;
                    if (nextCode == (1 << codeSize) && codeSize < maxCodeSize)
                    {
                        codeSize++;
                    }
                }

                previousEntry = entry;

                if (expectedOutputLength > 0 && output.Count >= expectedOutputLength)
                {
                    break;
                }
            }

            if (expectedOutputLength > 0 && output.Count != expectedOutputLength)
            {
                throw new InvalidDataException($"LZW decompression produced {output.Count} bytes, expected {expectedOutputLength}.");
            }

            return output.ToArray();
        }

        private static List<byte[]> CreateInitialDictionary()
        {
            List<byte[]> dictionary = new List<byte[]>(4096);
            for (int value = 0; value < 256; value++)
            {
                dictionary.Add(new byte[] { (byte)value });
            }

            dictionary.Add(Array.Empty<byte>());
            dictionary.Add(Array.Empty<byte>());
            return dictionary;
        }

        private static bool TryReadCode(byte[] bytes, ref int bitOffset, int codeSize, out int code)
        {
            if ((bitOffset + codeSize) > (bytes.Length * 8))
            {
                code = 0;
                return false;
            }

            code = 0;
            for (int bitIndex = 0; bitIndex < codeSize; bitIndex++)
            {
                int absoluteBitIndex = bitOffset + bitIndex;
                int byteIndex = absoluteBitIndex / 8;
                int bitWithinByte = absoluteBitIndex % 8;
                int bit = (bytes[byteIndex] >> bitWithinByte) & 0x01;
                code |= bit << bitIndex;
            }

            bitOffset += codeSize;
            return true;
        }

        private static byte[] Concatenate(byte[] prefix, byte suffix)
        {
            byte[] combined = new byte[prefix.Length + 1];
            Buffer.BlockCopy(prefix, 0, combined, 0, prefix.Length);
            combined[^1] = suffix;
            return combined;
        }
    }
}

using System.Buffers.Binary;

namespace U5.Core.IO
{
    /// <summary>
    /// Explicit little-endian reader over an in-memory byte buffer.
    /// </summary>
    public sealed class LittleEndianDataReader
    {
        private readonly byte[] _buffer;

        public LittleEndianDataReader(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        }

        public int Length
        {
            get
            {
                return _buffer.Length;
            }
        }

        public ushort ReadUInt16(int offset)
        {
            EnsureRange(offset, sizeof(ushort));
            return BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(offset, sizeof(ushort)));
        }

        public byte[] ReadBytes(int offset, int count)
        {
            EnsureRange(offset, count);
            byte[] result = new byte[count];
            Buffer.BlockCopy(_buffer, offset, result, 0, count);
            return result;
        }

        public byte[] Slice(int offset, int count)
        {
            return ReadBytes(offset, count);
        }

        private void EnsureRange(int offset, int count)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (offset + count > _buffer.Length)
            {
                throw new InvalidDataException($"Attempted to read past end of buffer. Offset={offset}, Count={count}, Length={_buffer.Length}.");
            }
        }
    }
}

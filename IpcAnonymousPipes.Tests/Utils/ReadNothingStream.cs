using System;
using System.IO;

namespace IpcAnonymousPipes.Tests.Utils
{
    /// <summary>
    /// The read method of string stream does not touch the buffer at all.
    /// </summary>
    class ReadNothingStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length { get; }

        private long _position;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public ReadNothingStream(long length)
        {
            Length = length;
        }

        public override void Flush()
        {
        }

        /// <summary>
        /// Advances the position within the stream, but does write 255 only at the very last position.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long remaining = Length - _position;
            if (count > remaining)
                count = (int)remaining;
            if (count <= 0)
                return 0;

            // Clear buffer on first read
            if (_position == 0)
            {
                for (int i = offset; i < count; i++)
                    buffer[i] = 0;
            }
            _position += count;

            // Write very last byte
            if (Length - _position == 0)
                buffer[offset + count - 1] = 255;

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}

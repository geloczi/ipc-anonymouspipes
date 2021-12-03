using System;
using System.IO;

namespace IpcAnonymousPipes.Tests.Mocks
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
        /// Advances the position within the stream, but does not write anything to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            long remaining = Length - Position;
            if (count > remaining)
                count = (int)remaining;
            _position += count;
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

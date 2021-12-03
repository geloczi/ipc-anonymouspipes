using System;
using System.IO;

namespace IpcAnonymousPipes.Tests.Mocks
{
    public class PipeStreamMock : Stream
    {
        private byte _nextValue;
        private long _position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => 0;

        public override long Position
        {
            get => _position;
            set => throw new NotImplementedException();
        }

        public int MaxBytesToRead { get; set; }
        public uint ReadCalls { get; private set; }

        public PipeStreamMock(int maxBytesToRead)
        {
            MaxBytesToRead = maxBytesToRead;
        }

        /// <summary>
        /// Writes a running byte value (0-255) to the buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            // A stream can read _less_ number of bytes than requested.
            // Example: HttpResponseStream.
            // This can be mocked by setting MaxBytesToRead to a lower value than count.
            ++ReadCalls;
            int read = Math.Min(MaxBytesToRead, count);
            for (int i = 0; i < read; i++)
            {
                buffer[i + offset] = _nextValue;
                unchecked
                {
                    ++_nextValue; // running value from 0 to 255
                }
            }
            _position += read;
            return read;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
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

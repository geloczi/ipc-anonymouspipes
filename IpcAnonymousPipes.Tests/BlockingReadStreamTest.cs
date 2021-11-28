using System;
using System.IO;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    public class BlockingReadStreamTest
    {
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(10, 5)]
        [TestCase(11, 5)]
        [TestCase(1000, 9)]
        public void BlockingReadStream_Read(int toRead, int maxBytesToRead)
        {
            using (var mock = new PipeStreamMock(maxBytesToRead))
            using (var prs = new BlockingReadStream(mock, toRead))
            {
                var buffer = new byte[Math.Max(1, toRead * 2)];
                int read = prs.Read(buffer, 0, toRead);

                // Check number of bytes
                Assert.AreEqual(toRead, read);
                // Check written part of the buffer
                AssertBufferRunningValue(buffer, 0, 0, toRead);
                // Check empty part of the buffer
                AssertBuffer(buffer, 0, toRead + 1, buffer.Length - toRead - 1);

                // Check the number of Read calls on the mock to make sure that the PipeReaderStream behaves correctly.
                int expectedReadCalls = (int)Math.Ceiling(toRead / (double)maxBytesToRead);
                Assert.AreEqual(expectedReadCalls, mock.ReadCalls);
            }
        }

        [TestCase(0, 0, 1)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(0, 10, 5)]
        [TestCase(0, 11, 5)]
        [TestCase(0, 1000, 9)]
        [TestCase(999, 1000, 9)]
        [TestCase(1000, 1000, 9)]
        public void BlockingReadStream_ReadToEnd(int readToEndFrom, int count, int maxBytesToRead)
        {
            using (var mock = new PipeStreamMock(maxBytesToRead))
            using (var prs = new BlockingReadStream(mock, count))
            {
                byte runningValueStart = 0;

                // ReadToEnd() can be called after several Read() calls, this snippet simulates this scenario.
                if (readToEndFrom > 0)
                {
                    byte[] buffer = new byte[readToEndFrom];
                    prs.Read(buffer, 0, readToEndFrom);
                    runningValueStart = buffer[buffer.Length - 1];
                    unchecked
                    {
                        ++runningValueStart;
                    }
                }

                // Read all bytes from the stream.
                byte[] data = prs.ReadToEnd();
                Assert.AreEqual(count, prs.Position);
                Assert.AreEqual(0, prs.RemainingBytes);
                Assert.AreEqual(count - readToEndFrom, data.Length);
                AssertBufferRunningValue(data, runningValueStart, 0, data.Length);
            }
        }

        [TestCase(0, 0, 1)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(0, 10, 5)]
        [TestCase(0, 11, 5)]
        [TestCase(0, 1000, 9)]
        [TestCase(999, 1000, 9)]
        [TestCase(1000, 1000, 9)]
        public void BlockingReadStream_ReadToEndDropBytes(int readToEndFrom, int count, int maxBytesToRead)
        {
            using (var mock = new PipeStreamMock(maxBytesToRead))
            using (var prs = new BlockingReadStream(mock, count))
            {
                // ReadToEnd() can be called after several Read() calls, this snippet simulates this scenario.
                if (readToEndFrom > 0)
                {
                    byte[] buffer = new byte[readToEndFrom];
                    prs.Read(buffer, 0, readToEndFrom);
                }

                // Drop remaining bytes (advances position in the stream without storing the data)
                prs.ReadToEndDropBytes();
                Assert.AreEqual(count, prs.Position);
                Assert.AreEqual(0, prs.RemainingBytes);
            }
        }

        private void AssertBufferRunningValue(byte[] buffer, byte startValue, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(startValue, buffer[i + offset]);
                unchecked
                {
                    ++startValue; // running value from 0 to 255
                }
            }
        }

        private void AssertBuffer(byte[] buffer, byte pattern, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                Assert.AreEqual(pattern, buffer[i + offset]);
        }

        class PipeStreamMock : Stream
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

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer is null)
                    throw new ArgumentNullException(nameof(buffer));
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset));
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                // Here is the thing. It is a real scenario that a stream can read _less_ number of bytes than requested.
                // The following code snippet simulates this case.
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
}

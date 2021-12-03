using System;
using System.IO;
using IpcAnonymousPipes.Tests.Mocks;
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
        public void Read(int toRead, int maxBytesToRead)
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

        [TestCase(0, -1)]
        [TestCase(1, -1)]
        public void Read_ArgumentOutOfRangeException(int streamLength, int toRead)
        {
            Exception exFromRead = null;
            using (var prs = new BlockingReadStream(new PipeStreamMock(streamLength), streamLength))
            {
                try
                {
                    prs.Read(new byte[Math.Abs(toRead)], 0, toRead);
                }
                catch (Exception ex)
                {
                    exFromRead = ex;
                }
            }
            Assert.IsInstanceOf<ArgumentOutOfRangeException>(exFromRead);
        }

        [TestCase]
        public void Read_EndOfStreamException()
        {
            Exception exFromRead = null;
            using (var prs = new BlockingReadStream(new PipeStreamMock(1), 1))
            {
                var buffer = new byte[1];
                prs.Read(buffer, 0, 1);
                try
                {
                    prs.Read(buffer, 0, 1);
                }
                catch (Exception ex)
                {
                    exFromRead = ex;
                }
            }
            Assert.IsInstanceOf<EndOfStreamException>(exFromRead);
        }

        [TestCase]
        public void Read_EndOfStreamException2()
        {
            Exception exFromRead = null;
            using (var prs = new BlockingReadStream(new PipeStreamMock(1), 0))
            {
                try
                {
                    prs.Read(new byte[1], 0, 1);
                }
                catch (Exception ex)
                {
                    exFromRead = ex;
                }
            }
            Assert.IsInstanceOf<EndOfStreamException>(exFromRead);
        }

        [TestCase(0, 0, 1)]
        [TestCase(0, 1, 1)]
        [TestCase(1, 1, 1)]
        [TestCase(0, 10, 5)]
        [TestCase(0, 11, 5)]
        [TestCase(0, 1000, 9)]
        [TestCase(999, 1000, 9)]
        [TestCase(1000, 1000, 9)]
        public void ReadToEnd(int readToEndFrom, int count, int maxBytesToRead)
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
        public void ReadToEndDropBytes(int readToEndFrom, int count, int maxBytesToRead)
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

        private static void AssertBufferRunningValue(byte[] buffer, byte startValue, int offset, int count)
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

        private static void AssertBuffer(byte[] buffer, byte pattern, int offset, int count)
        {
            for (int i = 0; i < count; i++)
                Assert.AreEqual(pattern, buffer[i + offset]);
        }
    }
}

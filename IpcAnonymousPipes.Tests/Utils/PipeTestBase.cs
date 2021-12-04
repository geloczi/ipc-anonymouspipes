using System.IO;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests.Utils
{
    public abstract class PipeTestBase
    {
        protected static void ReadStreamAssertLastByte(Stream stream, byte expectedLastByte)
        {
            Assert.AreEqual(stream.Length, stream.Length);
            var buffer = new byte[1024 * 32];
            int read = 0;
            while (stream.Position < stream.Length)
                read = stream.Read(buffer, 0, buffer.Length);
            Assert.Greater(read, 0);
            var lastByte = buffer[read - 1];
            Assert.AreEqual(expectedLastByte, lastByte);
        }
    }
}

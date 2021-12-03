using System;
using System.Threading;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class PipeClientTest
    {
        [Test]
        [NonParallelizable]
        public void SimplexClientToServer()
        {
            byte[] received = null;
            void Receive(BlockingReadStream stream)
            {
                received = stream.ReadToEnd();
            }

            using (var server = new PipeServer(false, Receive))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle, null))
            {
                server.RunAsync();
                client.Send(new byte[] { 255 });
            }

            Assert.IsNotNull(received);
            Assert.IsTrue(received.Length > 0);
            Assert.AreEqual(255, received[0]);
        }
    }
}

using System.Threading;
using IpcAnonymousPipes.Tests.Mocks;
using IpcAnonymousPipes.Tests.Utils;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class PipeClientTest : PipeTestBase
    {
        [Test]
        [NonParallelizable]
        public void SendLongStream()
        {
            long dataLength = ((long)int.MaxValue) + 1;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                server.WaitForClient();
                client.ReceiveAsync(s =>
                {
                    Assert.AreEqual(s.Length, dataLength);
                    ReadStreamAssertLastByte(s, 255);
                });

                // Send long stream
                using (var stream = new ReadNothingStream(dataLength))
                    server.Send(stream);

                server.WaitForTransmissionEnd();
            }
        }

        [Test]
        [NonParallelizable]
        public void SimplexClientToServer()
        {
            byte[] received = null;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                server.ReceiveAsync(s =>
                {
                    received = s.ReadToEnd();
                });

                // Calling client.RunAsync is not necessary in simplex communication.
                client.Send(new byte[] { 255 });
            }

            Assert.IsNotNull(received);
            Assert.IsTrue(received.Length > 0);
            Assert.AreEqual(255, received[0]);
        }

        [Test]
        [NonParallelizable]
        public void WaitForTransmissionEnd()
        {
            byte[] received = null;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                client.ReceiveAsync(s =>
                {
                    Thread.Sleep(100);
                    received = s.ReadToEnd();
                });

                server.WaitForClient();
                var sendThread = new Thread(() =>
                {
                    server.Send(new byte[] { 255 });
                });
                sendThread.IsBackground = true;
                sendThread.Start();
                sendThread.Join();

                client.WaitForTransmissionEnd();
            }
            Assert.IsNotNull(received);
            Assert.AreEqual(255, received[0]);
        }
    }
}

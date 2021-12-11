using System;
using System.Threading;
using IpcAnonymousPipes.Tests.Utils;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class PipeServerTest : PipeTestBase
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
                server.ReceiveAsync(s =>
                {
                    Assert.AreEqual(s.Length, dataLength);
                    ReadStreamAssertLastByte(s, 255);
                });

                // Send long stream
                using (var stream = new ReadNothingStream(dataLength))
                    client.Send(stream);

                server.WaitForTransmissionEnd();
            }
        }

        [Test]
        [NonParallelizable]
        public void SendLongStream2()
        {
            long dataLength = ((long)int.MaxValue) + 1;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                server.ReceiveAsync(s =>
                {
                    Assert.AreEqual(s.Length, dataLength);
                    ReadStreamAssertLastByte(s, 255);
                });
                server.WaitForClient();

                // Client sends long stream
                using (var stream = new ReadNothingStream(dataLength))
                    client.Send(stream);

                server.WaitForTransmissionEnd();
            }
        }

        [Test]
        [NonParallelizable]
        public void SimplexServerToClient()
        {
            byte[] received = null;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                client.ReceiveAsync(s =>
                {
                    received = s.ReadToEnd();
                });

                // Calling server.RunAsync is not necessary in simplex communication.
                server.WaitForClient();
                server.Send(new byte[] { 255 });
            }

            Assert.IsNotNull(received);
            Assert.IsTrue(received.Length > 0);
            Assert.AreEqual(255, received[0]);
        }

        [Test]
        [NonParallelizable]
        public void WaitForClient_TimeoutTest()
        {
            using (var server = new PipeServer())
            {
                server.ReceiveAsync(_ => { });
                try
                {
                    server.WaitForClient(10);
                    Assert.Fail($"Expected {nameof(TimeoutException)}");
                }
                catch (TimeoutException tex)
                {
                    Assert.AreEqual("Pipe client failed to connect within the specified amount of time.", tex.Message);
                }
            }
        }

        [Test]
        [NonParallelizable]
        public void WaitForClient_DisposeTest()
        {
            Exception exFromWaitForClient = null;

            // 1. Create pipe
            using (var server = new PipeServer())
            {
                server.ReceiveAsync(_ => { });

                // 2. This thread will be blocked on WaitForClient
                var waitThread = new Thread(() =>
                {
                    try
                    {
                        // 4. This thread will be blocked here until server.Dispose() call
                        server.WaitForClient(1000);
                    }
                    catch (Exception ex)
                    {
                        // 6. An exception will be thrown from WaitForClient after server.Dispose() call.
                        exFromWaitForClient = ex;
                    }
                });
                waitThread.IsBackground = true;
                waitThread.Start();

                // 3. Wait a little bit, so the thread enters the waiting loop
                Thread.Sleep(10);


                // 5. Dispose server while waitThread is still waiting for connection
                server.Dispose();
                waitThread.Join();
            }

            // 7. The thrown exception must be an ObjectDisposedException, since the server has been disposed.
            Assert.IsInstanceOf<ObjectDisposedException>(exFromWaitForClient);
        }

        [Test]
        [NonParallelizable]
        public void WaitForTransmissionEnd()
        {
            byte[] received = null;
            using (var server = new PipeServer(false))
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                server.ReceiveAsync(s =>
                {
                    Thread.Sleep(100);
                    received = s.ReadToEnd();
                });

                var sendThread = new Thread(() =>
                {
                    client.Send(new byte[] { 255 });
                });
                sendThread.IsBackground = true;
                sendThread.Start();
                sendThread.Join();

                server.WaitForTransmissionEnd();
            }
            Assert.IsNotNull(received);
            Assert.AreEqual(255, received[0]);
        }
    }
}

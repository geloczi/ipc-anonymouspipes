using System;
using System.Threading;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class PipeServerTest
    {
        [Test]
        [NonParallelizable]
        public void WaitForClient_Handles()
        {
            using (var server = new PipeServer(null))
            {
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
        public void WaitForClient_TimeoutDisposeTest()
        {
            Exception exFromWaitForClient = null;

            // 1. Create pipe
            using (var server = new PipeServer(null))
            {
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
        public void WaitForClient_TimeoutTest()
        {
            using (var server = new PipeServer(null))
            {
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

        //[Test]
        //[NonParallelizable]
        //public void WaitForTransmissionEnd()
        //{
        //    void Client_Receive(BlockingReadStream stream)
        //    {
        //        Thread.Sleep(1000);
        //    }

        //    using (var server = new PipeServer(false, _ => { }))
        //    using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle, Client_Receive))
        //    {
        //        server.RunAsync();
        //        client.RunAsync();
        //        server.WaitForClient(100);

        //        var sendThread = new Thread(() =>
        //        {
        //            server.Send(new byte[100]);
        //        });
        //        sendThread.IsBackground = true;
        //        sendThread.Start();

        //        server.WaitForTransmissionEnd();

        //    }
        //}
    }
}

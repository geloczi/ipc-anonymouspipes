using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
            using (var server = new PipeServer(_ => { }))
            {
                try
                {
                    server.WaitForClient(TimeSpan.FromMilliseconds(10));
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
            using (var server = new PipeServer(_ => { }))
            {
                // 2. This thread will be blocked on WaitForClient
                var waitThread = new Thread(() =>
                {
                    try
                    {
                        // 4. This thread will be blocked here until server.Dispose() call
                        server.WaitForClient(TimeSpan.FromSeconds(1));
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
            using (var server = new PipeServer(_ => { }))
            {
                try
                {
                    server.WaitForClient(TimeSpan.FromMilliseconds(10));
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
        //public void WaitForClient_TimeoutTest()
        //{
        //    // Test IPC using two Threads in the current Process
        //    using (var _server = new PipeServer(false, _ => { }))
        //    using (var _client = new PipeClient(_server.ClientInputHandle, _server.ClientOutputHandle, _ => { }))
        //    {
        //        // Start pipes
        //        _server.RunAsync();
        //        _client.RunAsync();
        //        _server.WaitForClient(TimeSpan.FromSeconds(1));

        //        // Wait until transmission finishes

        //    }
        //}
    }
}

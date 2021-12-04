using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using IpcAnonymousPipes.Tests.Utils;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class DuplexCommunicationTest : PipeTestBase
    {
        private static readonly Random Rnd = new Random();

        [Test]
        [NonParallelizable]
        public void SimpleTest()
        {
            Console.WriteLine($"TEST: {nameof(DuplexCommunicationTest)}");

            LocalTransmissionTest(new LocalTransmissionTestOptions()
            {
                TransmitAction = (o) =>
                {
                    o.ServerSend("Hello, this is your owner.");
                    o.ClientSend("Hi, I'm the client.");
                }
            });
        }

        [Test]
        [NonParallelizable]
        public void SlowClientTest()
        {
            Console.WriteLine($"TEST: {nameof(SlowClientTest)}");

            LocalTransmissionTest(new LocalTransmissionTestOptions()
            {
                ClientReceiveHook = () => Thread.Sleep(10),
                TransmitAction = (o) =>
                {
                    o.ServerSend("Hello, this is your owner.");
                    o.ClientSend("Hi, I'm the client.");
                }
            });
        }

        [Test]
        [NonParallelizable]
        public void StressTest()
        {
            Console.WriteLine($"TEST: {nameof(StressTest)}");

            int itemsToSend = 10000;

            LocalTransmissionTest(new LocalTransmissionTestOptions()
            {
                TransmitAction = (o) =>
                {
                    var clientSenderThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = 0; i < itemsToSend; i++)
                            o.ClientSend($"Client says {i}");
                        Console.WriteLine("Client send finished.");
                    }));
                    clientSenderThread.Start();
                    for (int i = 0; i < itemsToSend; i++)
                        o.ServerSend($"Server says {i}");
                    Console.WriteLine("Server send finished, joining client thread.");
                    clientSenderThread.Join();
                    Console.WriteLine("Client thread joined.");
                }
            });
        }

        [Test]
        [NonParallelizable]
        public void SlowClientStressTest()
        {
            Console.WriteLine($"TEST: {nameof(SlowClientStressTest)}");

            int itemsToSend = 100;

            LocalTransmissionTest(new LocalTransmissionTestOptions()
            {
                ClientReceiveHook = () => Thread.Sleep(1),
                TransmitAction = (o) =>
                {
                    var clientSenderThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = 0; i < itemsToSend; i++)
                            o.ClientSend($"Client says {i}");
                        Console.WriteLine("Client send finished.");
                    }));
                    clientSenderThread.Start();
                    for (int i = 0; i < itemsToSend; i++)
                        o.ServerSend($"Server says {i}");
                    Console.WriteLine("Server send finished, joining client thread.");
                    clientSenderThread.Join();
                    Console.WriteLine("Client thread joined.");
                }
            });
        }

        [Test]
        [NonParallelizable]
        public void RandomProcessingTimeTest()
        {
            Console.WriteLine($"TEST: {nameof(RandomProcessingTimeTest)}");

            int itemsToSend = 100;

            LocalTransmissionTest(new LocalTransmissionTestOptions()
            {
                ClientReceiveHook = () => Thread.Sleep(Rnd.Next(0, 20)),
                ServerReceiveHook = () => Thread.Sleep(Rnd.Next(0, 20)),
                TransmitAction = (o) =>
                {
                    var clientSenderThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = 0; i < itemsToSend; i++)
                            o.ClientSend($"Client says {i}");
                        Console.WriteLine("Client send finished.");
                    }));
                    clientSenderThread.Start();
                    for (int i = 0; i < itemsToSend; i++)
                        o.ServerSend($"Server says {i}");
                    Console.WriteLine("Server send finished, joining client thread.");
                    clientSenderThread.Join();
                    Console.WriteLine("Client thread joined.");
                }
            });
        }

        /// <summary>
        /// The skeleton to execute a transmission test locally in the current Process. 
        /// Data checks are implemented here, you just have to write the custom transmission scenario using the method argument.
        /// </summary>
        /// <param name="transmitAction">This is the action what is sending data to the server and client.</param>
        private static void LocalTransmissionTest(LocalTransmissionTestOptions options)
        {
            // Collections to hold the transmission data
            var serverSent = new List<string>();
            var clientReceived = new List<string>();
            var clientSent = new List<string>();
            var serverReceived = new List<string>();

            // Local methods
            void Server_Receive(BlockingReadStream stream)
            {
                if (!(options.ServerReceiveHook is null))
                    options.ServerReceiveHook();
                string text = Encoding.UTF8.GetString(stream.ReadToEnd());
                serverReceived.Add(text);
            }
            void Client_Receive(BlockingReadStream stream)
            {
                if (!(options.ClientReceiveHook is null))
                    options.ClientReceiveHook();
                string text = Encoding.UTF8.GetString(stream.ReadToEnd());
                clientReceived.Add(text);
            }

            // Test IPC using two Threads in the current Process
            using (var server = new PipeServer(false)) // Do not dispose the pipe handles, because the client is runnin in the same Process.
            using (var client = new PipeClient(server.ClientInputHandle, server.ClientOutputHandle))
            {
                // Local methods for data sending
                void ServerSend(string data)
                {
                    serverSent.Add(data);
                    server.Send(Encoding.UTF8.GetBytes(data));
                }
                void ClientSend(string data)
                {
                    clientSent.Add(data);
                    client.Send(Encoding.UTF8.GetBytes(data));
                }

                // Start pipes
                server.ReceiveAsync(Server_Receive);
                client.ReceiveAsync(Client_Receive);
                server.WaitForClient(TimeSpan.FromSeconds(1));

                // Execute test transmission
                options.TransmitAction(new TransmitActionArgs()
                {
                    ServerSend = ServerSend,
                    ClientSend = ClientSend
                });

                // Wait until transmission finishes
                server.WaitForTransmissionEnd();
                client.WaitForTransmissionEnd();
                Console.WriteLine($"Transmission finished, pipes are drained.");
            }

            Console.WriteLine($"serverSent: {serverSent.Count}");
            Console.WriteLine($"clientReceived: {clientReceived.Count}");
            Console.WriteLine($"clientSent: {clientSent.Count}");
            Console.WriteLine($"serverReceived: {serverReceived.Count}");

            // Compare byte to byte (server to client)
            for (int i = 0; i < serverSent.Count; i++)
                Assert.IsTrue(serverSent[i].SequenceEqual(clientReceived[i]));
            // Compare byte to byte (client to server)
            for (int i = 0; i < clientSent.Count; i++)
                Assert.IsTrue(clientSent[i].SequenceEqual(serverReceived[i]));
        }

        class TransmitActionArgs
        {
            public Action<string> ServerSend { get; set; }
            public Action<string> ClientSend { get; set; }
        }

        class LocalTransmissionTestOptions
        {
            public Action ClientReceiveHook;
            public Action ServerReceiveHook;
            public Action<TransmitActionArgs> TransmitAction;
        }
    }
}
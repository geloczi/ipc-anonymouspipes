using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace IpcAnonymousPipes.Tests
{
    [NonParallelizable]
    public class PipeTests
    {
        private static readonly Random Rnd = new Random();

        class LocalTransmissionTestParams
        {
            public Action<string> ServerSend { get; set; }
            public Action<string> ClientSend { get; set; }
        }

        [Test]
        [NonParallelizable]
        public void DuplexCommunicationTest()
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

        class LocalTransmissionTestOptions
        {
            public Action ClientReceiveHook;
            public Action ServerReceiveHook;
            public Action<LocalTransmissionTestParams> TransmitAction;
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
            using (var _server = new PipeServer(false, Server_Receive)) // Do not dispose the pipe handles, because the client is runnin in the same Process.
            using (var _client = new PipeClient(_server.ClientInputHandle, _server.ClientOutputHandle, Client_Receive))
            {
                // Local methods for data sending
                void ServerSend(string data)
                {
                    serverSent.Add(data);
                    _server.Send(Encoding.UTF8.GetBytes(data));
                }
                void ClientSend(string data)
                {
                    clientSent.Add(data);
                    _client.Send(Encoding.UTF8.GetBytes(data));
                }

                // Start pipes
                _server.RunAsync();
                _client.RunAsync();
                _server.WaitForClient(TimeSpan.FromSeconds(1));

                // Execute test transmission
                options.TransmitAction(new LocalTransmissionTestParams()
                {
                    ServerSend = ServerSend,
                    ClientSend = ClientSend
                });

                // Wait until transmission finishes
                _server.WaitForTransmissionEnd();
                _client.WaitForTransmissionEnd();
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

    }
}
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace IpcAnonymousPipes
{
    /// <summary>
    /// IPC server
    /// </summary>
    public class PipeServer : PipeCommon, IDisposable
    {
        private readonly bool _disposeLocalCopyOfHandlesAfterClientConnected;
        private readonly AnonymousPipeServerStream _inPipe = new AnonymousPipeServerStream(PipeDirection.In, HandleInheritability.Inheritable);
        private readonly AnonymousPipeServerStream _outPipe = new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
        private Thread _workerThread;

        /// <summary>
        /// This is "PipeDirection.Out" for the client
        /// </summary>
        public string ClientOutputHandle { get; }

        /// <summary>
        /// This is "PipeDirection.In" for the client
        /// </summary>
        public string ClientInputHandle { get; }

        /// <summary>
        /// Creates a server instance
        /// </summary>
        public PipeServer(Action<BlockingReadStream> receiveAction) : this(true, receiveAction)
        {
        }

        /// <summary>
        /// Creates a server instance
        /// </summary>
        /// <param name="disposeLocalCopyOfHandlesAfterClientConnect">True to dispose the local copy of handles after the pipe client is connected. False to keep the handles, so you can use the server and client in the same Process (for example: inter-thread communication, unit testing)</param>
        /// <param name="receiveAction">Method to call when data packet received</param>
        public PipeServer(bool disposeLocalCopyOfHandlesAfterClientConnect, Action<BlockingReadStream> receiveAction)
            : base(receiveAction)
        {
            ClientOutputHandle = _inPipe.GetClientHandleAsString();
            ClientInputHandle = _outPipe.GetClientHandleAsString();
            _disposeLocalCopyOfHandlesAfterClientConnected = disposeLocalCopyOfHandlesAfterClientConnect;
            // At this point, the pipe exists and the client side can connect with the handles
        }

        /// <summary>
        /// Runs the messaging on a new thread without blocking the caller.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void RunAsync()
        {
            Ensure();
            if (!(_workerThread is null))
                throw new InvalidOperationException("Server is running");

            _workerThread = new Thread(new ThreadStart(Run))
            {
                Name = nameof(PipeServer),
                IsBackground = true
            };
            _workerThread.Start();
        }

        /// <summary>
        /// Runs the messaging on the current thread, so blocks until the pipe is closed.
        /// </summary>
        public void Run()
        {
            Ensure();

            // Read control byte from the client
            // Blocks the thread until a control byte arrives
            int control = _inPipe.ReadByte();
            if (control == ControlByte.Connect)
            {
                // Client is connected, we can dispose our local copy of the handles now
                IsConnected = true;
                if (_disposeLocalCopyOfHandlesAfterClientConnected)
                {
                    _inPipe.DisposeLocalCopyOfClientHandle();
                    _outPipe.DisposeLocalCopyOfClientHandle();
                }

                // Receive messages
                ReceiverMethod(_inPipe);
            }
            else
            {
                // Somwething went wrong and the pipe cannot be used, so dispose it
                Dispose();
            }
        }

        /// <summary>
        /// Sends bytes to the pipe
        /// </summary>
        /// <param name="data"></param>
        public override void Send(byte[] data)
        {
            SendData(_outPipe, data);
        }

        /// <summary>
        /// Wait until pipes finish transmission
        /// </summary>
        public override void WaitForTransmissionEnd()
        {
            Ensure();
            lock (_syncRoot)
            {
                _outPipe.WaitForPipeDrain();
                WaitForReceive();
            }
        }

        /// <summary>
        /// Wait until connection established
        /// </summary>
        /// <param name="timeout"></param>
        /// <exception cref="TimeoutException"></exception>
        /// <exception cref="Exception"></exception>
        public void EnsureConnection(TimeSpan timeout)
        {
            Ensure();
            var sw = new Stopwatch();
            sw.Start();
            while (!IsConnected && !_disposed)
            {
                if (sw.Elapsed >= timeout)
                    throw new TimeoutException(nameof(PipeServer));
                Thread.Sleep(1);
            }
            Ensure();
            if (!IsConnected)
                throw new Exception("Pipe client failed to connect.");
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            // Try to send a disconnect message. This might fail if the pipe is already broken.
            try { SendDisconnect(_outPipe); }
            catch { }

            if (!_disposeLocalCopyOfHandlesAfterClientConnected)
            {
                // Dispose local copy of client handles. 
                // These might fail if the client lived in the same Process (unit tests for example), so disposing the client disposed the handle already.
                try { _inPipe.DisposeLocalCopyOfClientHandle(); }
                catch { }
                try { _outPipe.DisposeLocalCopyOfClientHandle(); }
                catch { }
            }

            try
            {
                _inPipe.Dispose();
                _outPipe.Dispose();
            }
            catch { }
        }

        /// <summary>
        /// Checks connection.
        /// </summary>
        /// <returns></returns>
        protected override bool PipesAreConnected()
        {
            return _inPipe?.IsConnected == true && _outPipe?.IsConnected == true;
        }

    }
}

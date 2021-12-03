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
        public PipeServer() : this(true)
        {
        }

        /// <summary>
        /// Creates a server instance
        /// </summary>
        /// <param name="disposeLocalCopyOfHandlesAfterClientConnect">True to dispose the local copy of handles after the pipe client is connected. False to keep the handles, so you can use the server and client in the same Process (for example: inter-thread communication, unit testing)</param>
        public PipeServer(bool disposeLocalCopyOfHandlesAfterClientConnect)
            : base()
        {
            ClientOutputHandle = _inPipe.GetClientHandleAsString();
            ClientInputHandle = _outPipe.GetClientHandleAsString();
            _disposeLocalCopyOfHandlesAfterClientConnected = disposeLocalCopyOfHandlesAfterClientConnect;
            // At this point, the pipe exists and the client side can connect with the handles
        }

        /// <summary>
        /// Runs the messaging on the current thread, so blocks until the pipe is closed.
        /// </summary>
        protected override void ReceiveInternal()
        {
            if (ReadConnectByte())
            {
                ReceiverLoop(_inPipe);
            }
            else
            {
                // Somwething went wrong and the pipe cannot be used, so dispose it
                Dispose();
            }
        }

        private bool ReadConnectByte()
        {
            // Read control byte from the client
            // Blocks the thread until a control byte arrives
            int control = _inPipe.ReadByte();

            // Client sent something, so we can dispose our local copy of the handles now
            if (_disposeLocalCopyOfHandlesAfterClientConnected)
            {
                _inPipe.DisposeLocalCopyOfClientHandle();
                _outPipe.DisposeLocalCopyOfClientHandle();
            }

            // A connect byte is expected
            IsConnected = control == ControlByte.Connect;
            return IsConnected;
        }

        /// <summary>
        /// Sends bytes to the pipe
        /// </summary>
        /// <param name="data"></param>
        public override void Send(byte[] data)
        {
            SendBytes(_outPipe, data);
        }

        /// <summary>
        /// Sends bytes to the pipe
        /// </summary>
        /// <param name="stream"></param>
        public override void Send(Stream stream)
        {
            SendStream(_outPipe, stream);
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
                WaitForReceiveOrSend();
            }
        }

        /// <summary>
        /// Blocks the caller until client gets connected.
        /// </summary>
        /// <exception cref="Exception">Thrown on unknown connection failure.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed during the waiting.</exception>
        /// <exception cref="IOException">Thrown when the pipes are broken.</exception>
        public void WaitForClient() => WaitForClient(TimeSpan.Zero);

        /// <summary>
        /// Blocks the caller until client gets connected.
        /// </summary>
        /// <param name="milliseconds">Maximum amount of time in milliseconds to wait for the client.</param>
        /// <exception cref="TimeoutException">Thrown when the client fails to connect in the specified amount of time.</exception>
        /// <exception cref="Exception">Thrown on unknown connection failure.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed during the waiting.</exception>
        /// <exception cref="IOException">Thrown when the pipes are broken.</exception>
        public void WaitForClient(int milliseconds) => WaitForClient(TimeSpan.FromMilliseconds(milliseconds));

        /// <summary>
        /// Blocks the caller until client gets connected.
        /// </summary>
        /// <param name="timeout">Maximum amount of time to wait for the client.</param>
        /// <exception cref="TimeoutException">Thrown when the client fails to connect in the specified amount of time.</exception>
        /// <exception cref="Exception">Thrown on unknown connection failure.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this instance has been disposed during the waiting.</exception>
        /// <exception cref="IOException">Thrown when the pipes are broken.</exception>
        public void WaitForClient(TimeSpan timeout)
        {
            Ensure();
            if (ReceiverStarted)
            {
                var sw = new Stopwatch();
                sw.Start();
                while (!IsConnected && !_disposed)
                {
                    if (timeout != TimeSpan.Zero && sw.Elapsed >= timeout)
                        throw new TimeoutException("Pipe client failed to connect within the specified amount of time.");
                    Thread.Sleep(1);
                    Ensure();
                }
            }
            else
            {
                if (timeout != TimeSpan.Zero)
                    throw new ArgumentException($"Can't use timeout without calling {nameof(ReceiveAsync)} or {nameof(Receive)} first.", nameof(timeout));
                ReadConnectByte();
            }
            if (!IsConnected)
                throw new Exception("Failed to connect.");
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

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace IpcAnonymousPipes
{
    /// <summary>
    /// PipeCommon
    /// </summary>
    public abstract class PipeCommon : IDisposable
    {
        #region Constants

        /// <summary>
        /// Input pipe handle command line argument prefix.
        /// </summary>
        protected const string InPipeHandleArg = "--InPipeHandle=";

        /// <summary>
        /// Output pipe handle command line argument prefix.
        /// </summary>
        protected const string OutPipeHandleArg = "--OutPipeHandle=";

        #endregion Constants

        #region Fields

        private int _controlByte;
        private bool _isSending;
        private Thread _receiverThread;

        /// <summary>
        /// Synchronization object
        /// </summary>
        protected readonly object _syncRoot = new object();

        /// <summary>
        /// Method to call when data packet received
        /// </summary>
        protected Action<BlockingReadStream> _receiveAction;

        #endregion Fields

        #region Events

        /// <summary>
        /// Raised when an error occured.
        /// </summary>
        public event EventHandler<Exception> OnError;

        /// <summary>
        /// Raised when connected.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Raised when disconnected.
        /// </summary>
        public event EventHandler Disconnected;

        #endregion Events

        #region Properties

        /// <summary>
        /// Disposed
        /// </summary>
        public bool IsDisposed { get; private set; }

        private bool _isConnected;
        /// <summary>
        /// Indicates wether this pipe is connected or not.
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected && !IsDisposed;
            protected set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    if (IsConnected)
                        RaiseConnected();
                    else
                        RaiseDisconnected();
                }
            }
        }

        /// <summary>
        /// Indicates wether the receiver loop started or not.
        /// </summary>
        protected bool ReceiverStarted { get; private set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Sends the specified byte array into the pipe.
        /// </summary>
        /// <param name="data"></param>
        public abstract void Send(byte[] data);

        /// <summary>
        /// Sends the specified stream into the pipe.
        /// </summary>
        /// <param name="stream"></param>
        public abstract void Send(Stream stream);

        /// <summary>
        /// Blocks the calling thread until all data transmission finishes.
        ///  With this method, you can ensure that all data have arrived before disposing the pipe.
        /// </summary>
        public abstract void WaitForTransmissionEnd();

        /// <summary>
        /// Run
        /// </summary>
        /// <param name="receiveAction">Method to call when data packet received</param>
        public void Receive(Action<BlockingReadStream> receiveAction)
        {
            if (ReceiverStarted || !(_receiverThread is null))
                throw new InvalidOperationException("Already receiving.");
            if (receiveAction is null)
                throw new ArgumentNullException(nameof(receiveAction));
            _receiveAction = receiveAction;
            
            Ensure();
            ReceiverStarted = true;
            ReceiveInternal();
        }

        /// <summary>
        /// RunAsync
        /// </summary>
        /// <param name="receiveAction">Method to call when data packet received</param>
        public void ReceiveAsync(Action<BlockingReadStream> receiveAction)
        {
            if (ReceiverStarted || !(_receiverThread is null))
                throw new InvalidOperationException("Already receiving.");
            if (receiveAction is null)
                throw new ArgumentNullException(nameof(receiveAction));
            _receiveAction = receiveAction;

            Ensure();
            ReceiverStarted = true;
            _receiverThread = new Thread(new ThreadStart(ReceiveInternal))
            {
                Name = nameof(PipeServer),
                IsBackground = true
            };
            _receiverThread.Start();
        }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed)
                return;
            OnDispose();
            IsDisposed = true;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        /// Called on disposing.
        /// </summary>
        protected abstract void OnDispose();

        /// <summary>
        /// The receiver method.
        /// </summary>
        protected abstract void ReceiveInternal();

        /// <summary>
        /// Checks connection.
        /// </summary>
        /// <returns></returns>
        protected abstract bool PipesAreConnected();

        /// <summary>
        /// Sends disconnect control byte
        /// </summary>
        /// <param name="pipe"></param>
        protected void SendDisconnect(PipeStream pipe)
        {
            lock (_syncRoot)
            {
                // Control byte
                pipe.WriteByte(ControlByte.Disconnect);
                pipe.Flush();
            }
        }

        /// <summary>
        /// Sends data packet
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        protected void SendBytes(PipeStream pipe, byte[] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));
            if (data.Length == 0)
                return;

            lock (_syncRoot)
            {
                _isSending = true;
                try
                {
                    Ensure();
                    // Control byte
                    pipe.WriteByte(ControlByte.Data);
                    // Length bytes
                    var lengthBytes = BitConverter.GetBytes((long)data.Length);
                    if (lengthBytes.Length != 8)
                        throw new Exception($"BitConverter.GetBytes returned unexpected number of bytes: {lengthBytes.Length}");
                    pipe.Write(lengthBytes, 0, lengthBytes.Length);
                    // Data bytes
                    pipe.Write(data, 0, data.Length);
                    pipe.Flush();
                    pipe.WaitForPipeDrain();
                }
                finally
                {
                    _isSending = false;
                }
            }
        }

        /// <summary>
        /// Sends data packet
        /// </summary>
        /// <param name="pipe"></param>
        /// <param name="stream"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        protected void SendStream(PipeStream pipe, Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (stream.Length <= 0)
                return;

            lock (_syncRoot)
            {
                _isSending = true;
                try
                {
                    Ensure();
                    // Control byte
                    pipe.WriteByte(ControlByte.Data);
                    // Length bytes
                    var lengthBytes = BitConverter.GetBytes((long)stream.Length);
                    if (lengthBytes.Length != 8)
                        throw new Exception($"BitConverter.GetBytes returned unexpected number of bytes: {lengthBytes.Length}");
                    pipe.Write(lengthBytes, 0, lengthBytes.Length);

                    // Data bytes
                    stream.CopyTo(pipe);
                    //var buffer = new byte[1024 * 64];
                    //while (stream.Position < stream.Length)
                    //{
                    //    int read = stream.Read(buffer, 0, buffer.Length);
                    //    pipe.Write(buffer, 0, read);
                    //    pipe.Flush();
                    //    pipe.WaitForPipeDrain();
                    //}
                }
                finally
                {
                    _isSending = false;
                }
            }
        }

        /// <summary>
        /// Raises the OnError event
        /// </summary>
        /// <param name="ex"></param>
        protected void RaiseOnError(Exception ex)
        {
            if (!(OnError is null))
                OnError(this, ex);
        }

        /// <summary>
        /// Receiver loop, reads data from the pipe
        /// </summary>
        /// <param name="pipe"></param>
        protected void ReceiverLoop(PipeStream pipe)
        {
            try
            {
                var buffer = new byte[4096];
                while (!IsDisposed && pipe.IsConnected)
                {
                    _controlByte = pipe.ReadByte();
                    if (_controlByte < 0 || _controlByte == ControlByte.Disconnect)
                    {
                        // Disconnect
                        break;
                    }
                    else if (_controlByte == ControlByte.Data)
                    {
                        // Data packet, read length
                        EnsureRead(pipe, buffer, 0, 8);
                        long length = BitConverter.ToInt64(buffer, 0);

                        // Read data bytes
                        var blockingReadStream = new BlockingReadStream(pipe, length);
                        if (!(_receiveAction is null))
                        {
                            // The receive action will perform the read operation using the BlockingReadStream which wraps around our pipe.
                            try
                            {
                                _receiveAction(blockingReadStream);
                            }
                            catch (Exception ex)
                            {
                                RaiseOnError(ex);
                            }
                        }
                        // Drop remaining data bytes in the case the receive action didn't finish reading for any reason.
                        blockingReadStream.ReadToEndDropBytes();
                    }
                    _controlByte = 0; //Flip the internal state back to zero after a transmit
                }
                IsConnected = false;
            }
            catch (ThreadAbortException) { }
            catch (Exception ex)
            {
                RaiseOnError(ex);
            }
        }

        /// <summary>
        /// Waits for data transmission end.
        /// </summary>
        protected void WaitForReceiveOrSend()
        {
            while (_controlByte >= ControlByte.Data || _isSending)
                Thread.Sleep(1);
        }

        /// <summary>
        /// Ensures the connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="IOException"></exception>
        protected void Ensure()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
            if (!PipesAreConnected())
                throw new IOException("Pipes are broken.");
        }

        #endregion Protected Methods

        #region Private Methods

        private void RaiseConnected()
        {
            if (!(Connected is null))
                Connected(this, new EventArgs());
        }

        private void RaiseDisconnected()
        {
            if (!(Disconnected is null))
                Disconnected(this, new EventArgs());
        }

        /// <summary>
        /// Reads exactly the specified amount of bytes from the stream (count). 
        /// It will block the caller thread when there is not enough data and will wait to get all the bytes from the stream.
        /// </summary>
        /// <param name="stream">Source stream to read.</param>
        /// <param name="buffer">Buffer to write.</param>
        /// <param name="offset">Buffer offset.</param>
        /// <param name="count">Number of bytes to read.</param>
        private static void EnsureRead(Stream stream, byte[] buffer, int offset, int count)
        {
            int read;
            while (count > 0)
            {
                read = stream.Read(buffer, offset, count);
                offset += read;
                count -= read;
            }
        }

        #endregion Private Methods

        #region Classes

        /// <summary>
        /// Message header is a control byte which defines the current operation.
        /// </summary>
        protected static class ControlByte
        {
            /// <summary>
            /// Connection established, ready to receive.
            /// </summary>
            public const byte Connect = 1;

            /// <summary>
            /// Connection closed, stop transmission.
            /// </summary>
            public const byte Disconnect = 2;

            /// <summary>
            /// Data packet is being sent.
            /// </summary>
            public const byte Data = 3;
        }

        #endregion
    }
}

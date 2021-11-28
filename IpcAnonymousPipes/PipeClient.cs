using System;
using System.IO.Pipes;
using System.Threading;

namespace IpcAnonymousPipes
{
    /// <summary>
    /// IPC client
    /// </summary>
    public class PipeClient : PipeCommon, IDisposable
    {
        private readonly AnonymousPipeClientStream _inPipe;
        private readonly AnonymousPipeClientStream _outPipe;
        private Thread _workerThread;

        /// <summary>
        /// Creates a new instance of PipeClient
        /// </summary>
        /// <param name="inputPipeHandle"></param>
        /// <param name="outputPipeHandle"></param>
        /// <param name="receiveAction"></param>
        public PipeClient(string inputPipeHandle, string outputPipeHandle, Action<BlockingReadStream> receiveAction)
            : base(receiveAction)
        {
            _inPipe = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            _outPipe = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);

            // Send connect byte to the pipe server, so it will know that the client is alive.
            _outPipe.WriteByte(ControlByte.Connect);
            _outPipe.Flush();
            IsConnected = true;
        }

        /// <summary>
        /// Runs the messaging on a new thread without blocking the caller.
        /// </summary>
        public void RunAsync()
        {
            Ensure();
            _workerThread = new Thread(new ThreadStart(Run))
            {
                Name = nameof(PipeClient),
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
            _outPipe.WaitForPipeDrain(); // Make sure that the pipe server received the connect byte
            ReceiverMethod(_inPipe);
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
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            try
            {
                SendDisconnect(_outPipe);
            }
            catch { }
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

using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
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

        /// <summary>
        /// Creates a new instance of PipeClient.
        /// </summary>
        /// <param name="inputPipeHandle"></param>
        /// <param name="outputPipeHandle"></param>
        public PipeClient(string inputPipeHandle, string outputPipeHandle)
        {
            _inPipe = new AnonymousPipeClientStream(PipeDirection.In, inputPipeHandle);
            _outPipe = new AnonymousPipeClientStream(PipeDirection.Out, outputPipeHandle);

            // Send connect byte to the pipe server, so it will know that the client is alive.
            _outPipe.WriteByte(ControlByte.Connect);
            _outPipe.Flush();
            IsConnected = true;
        }

        /// <summary>
        /// Creates a new instance of PipeClient. Parses pipe handles automatically from the command line arguments.
        /// </summary>
        public PipeClient()
            : this(ParseCommandLineArg(InPipeHandleArg), ParseCommandLineArg(OutPipeHandleArg))
        {
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

        /// <summary>
        /// Runs the messaging on the current thread, so blocks until the pipe is closed.
        /// </summary>
        protected override void ReceiveInternal()
        {
            _outPipe.WaitForPipeDrain(); // Make sure that the pipe server received the connect byte
            ReceiverLoop(_inPipe);
        }

        private static string ParseCommandLineArg(string prefix)
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string value = args.First(x => x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Remove(0, prefix.Length);
                return value;
            }
            catch
            {
                throw new ArgumentException($"Cannot parse command line argument: {prefix}");
            }
        }
    }
}

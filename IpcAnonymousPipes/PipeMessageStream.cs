﻿using System;
using System.IO;

namespace IpcAnonymousPipes
{
    /// <summary>
    /// Wraps a stream and provides blocking read operation.
    /// </summary>
    public class PipeMessageStream : Stream
    {
        private long _position;
        private readonly Stream _stream;

        /// <summary>
        /// Gets a value indicating whether the current stream supports reading.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the current stream supports seeking.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the current stream supports writing.
        /// </summary>
        public override bool CanWrite => false;

        /// <summary>
        /// Gets the length in bytes of the stream.
        /// </summary>
        public override long Length { get; }

        /// <summary>
        /// Gets the position within the current stream.
        /// </summary>
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Number of bytes to read.
        /// </summary>
        public long RemainingBytes => (long)(Length - Position);

        /// <summary>
        /// Creates a new instance of BlockingReadStream
        /// </summary>
        /// <param name="stream">The underlying stream</param>
        /// <param name="length">Length of the data.</param>
        /// <exception cref="ArgumentException"></exception>
        public PipeMessageStream(Stream stream, long length)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead)
                throw new ArgumentException("The source stream is not readable.", nameof(stream));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            _stream = stream;
            Length = length;
        }

        /// <summary>
        /// Has no effect.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Reads the specified number of bytes from the stream.
        /// </summary>
        /// <param name="buffer">Target byte array to write into.</param>
        /// <param name="offset">Buffer offset.</param>
        /// <param name="count">Number of bytes to read from the stream. Must be smaller or equal to the Length of the stream.</param>
        /// <returns>Returns the number of bytes ridden from the stream.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (RemainingBytes == 0)
                throw new EndOfStreamException();
            if (count > RemainingBytes)
                count = (int)RemainingBytes;

            int readPosition = offset;
            int readOffset = count;
            int read;
            while (readOffset > 0)
            {
                read = _stream.Read(buffer, readPosition, readOffset);
                readPosition += read;
                readOffset -= read;
            }

            _position += count;
            return count;
        }

        /// <summary>
        /// Reads all bytes from the stream into memory.
        /// </summary>
        /// <returns>Byte array containing the data.</returns>
        public byte[] ReadToEnd()
        {
            if (RemainingBytes == 0)
                return new byte[0];
            if (RemainingBytes > int.MaxValue)
                throw new NotSupportedException("The stream is too long to read it into a byte array.");
            byte[] data = new byte[RemainingBytes];
            Read(data, 0, (int)RemainingBytes);
            return data;
        }

        /// <summary>
        /// Reads the underlying stream to end without storing the bytes.
        /// </summary>
        public void ReadToEndDropBytes()
        {
            if (RemainingBytes == 0)
                return;
            byte[] buffer = new byte[4096];
            while (RemainingBytes > 0)
                Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="value"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <exception cref="NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}

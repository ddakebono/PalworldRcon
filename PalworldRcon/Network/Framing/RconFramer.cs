using System;

namespace PalworldRcon.Network.Framing;

public class RconFramer : IMessageFramer
{
    /// <summary>
    /// Called every time ReceiveData got a full message.
    /// </summary>
    public event Action<byte[]> MessageReceived;
    public int MaxMessageSize; //Rcon protocol spec

    private readonly byte[] _lengthBuffer;
    private byte[] _messageBuffer;
    private int _bytesReceived;

    public RconFramer(int maxMessageSize = 4096)
    {
        MaxMessageSize = maxMessageSize;
        _lengthBuffer = new byte[sizeof(int)];
    }

    public byte[] Frame(byte[] message)
    {
        if (message.Length > MaxMessageSize)
            throw new InvalidMessageSizeException("Invalid size (" + message.Length + ").");

        var dataLength = message.Length + 1; //Size int + null term byte
        var result = new byte[dataLength + sizeof(int)];

        result[0] = (byte)(dataLength >> (8 * 0));
        result[1] = (byte)(dataLength >> (8 * 1));
        result[2] = (byte)(dataLength >> (8 * 2));
        result[3] = (byte)(dataLength >> (8 * 3));
        result[dataLength - 1] = 0;

        Buffer.BlockCopy(message, 0, result, 4, message.Length);

        return result;
    }

    /// <summary>
    /// Receives data and calls MessageReceived every time a full message
    /// has arrived.
    /// </summary>
    /// <param name="data">Buffer to read from.</param>
    /// <param name="length">Length of actual information in data.</param>
    /// <exception cref="InvalidMessageSizeException">
    /// Thrown if a message has an invalid size. Should this occur,
    /// the connection should be terminated, because it's not save to
    /// keep receiving anymore.
    /// </exception>
    public void ReceiveData(byte[] data, int length)
    {
        var bytesAvailable = length;
        if (bytesAvailable == 0)
            return;

        for (var i = 0; i < bytesAvailable;)
        {
            if (_messageBuffer == null)
            {
                var read = Math.Min(_lengthBuffer.Length - _bytesReceived, bytesAvailable - i);
                Buffer.BlockCopy(data, i, _lengthBuffer, _bytesReceived, read);

                _bytesReceived += read;
                i += read;

                if (_bytesReceived == _lengthBuffer.Length)
                {
                    var messageSize = BitConverter.ToInt32(data, 0);

                    if (messageSize < 0 || messageSize > this.MaxMessageSize)
                        throw new InvalidMessageSizeException("Invalid size (" + messageSize + ").");

                    _messageBuffer = new byte[messageSize];
                    _bytesReceived = 0;
                }
            }

            if (_messageBuffer != null)
            {
                var read = Math.Min(_messageBuffer.Length - _bytesReceived, bytesAvailable - i);
                Buffer.BlockCopy(data, i, _messageBuffer, _bytesReceived, read);

                _bytesReceived += read;
                i += read;

                if (_bytesReceived == _messageBuffer.Length)
                {
                    MessageReceived?.Invoke(_messageBuffer);

                    _messageBuffer = null;
                    _bytesReceived = 0;
                }
            }
        }
    }

    /// <summary>
    /// An exception that might occur if a message has an invalid size.
    /// </summary>
    public class InvalidMessageSizeException : Exception
    {
        /// <summary>
        /// Creates new instance.
        /// </summary>
        /// <param name="message"></param>
        public InvalidMessageSizeException(string message)
            : base(message)
        {
        }
    }
}
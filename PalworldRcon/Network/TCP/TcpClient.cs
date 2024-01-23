using System;
using System.Net;
using System.Net.Sockets;

namespace PalworldRcon.Network.TCP
{
	/// <summary>
	/// A TCP client used to connect to TCP servers.
	/// </summary>
	public abstract class TcpClient
	{
		private const int BufferMaxSize = 8 * 1024;

		private byte[] _buffer = new byte[BufferMaxSize];
		private Socket _socket;

		/// <summary>
		/// Current status of the connection.
		/// </summary>
		public ClientStatus Status { get; private set; }

		/// <summary>
		/// Address of the local end point.
		/// </summary>
		public string LocalAddress { get; private set; }

		/// <summary>
		/// Address of the remote end point.
		/// </summary>
		public string RemoteAddress { get; private set; }

		/// <summary>
		/// Raised when an exception occurs while receiving data.
		/// </summary>
		public event Action<TcpClient, Exception> ReceiveException;

		/// <summary>
		/// Raised when connection was closed.
		/// </summary>
		public event Action<TcpClient, ConnectionCloseType> Disconnected;

		/// <summary>
		/// Raised when connection is opened
		/// </summary>
		public event Action<TcpClient> Connected;

		/// <summary>
		/// Raised when an exception occurs during connection
		/// </summary>
		public event Action<Exception> ConnectionFailed;

		/// <summary>
		/// Last error received while working asynchronously.
		/// </summary>
		public string LastError { get; private set; }

		/// <summary>
		/// Last exception received while working asynchronously.
		/// </summary>
		public Exception LastException { get; private set; }

		/// <summary>
		/// Connects to host.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public void Connect(string host, int port)
		{
			try
			{
				this.Connect(new IPEndPoint(IPAddress.Parse(host), port));
			}
			catch
			{
				var ips = Dns.GetHostAddresses(host);
				if(ips.Length > 0)
					this.Connect(new IPEndPoint(ips[0], port));
			}
		}

		/// <summary>
		/// Connects to remote end point.
		/// </summary>
		/// <param name="remoteEndPoint"></param>
		public void Connect(IPEndPoint remoteEndPoint)
		{
			if (_socket != null)
				this.Disconnect();

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.Connect(remoteEndPoint);

			this.Status = ClientStatus.Connected;
			this.LocalAddress = ((IPEndPoint)_socket.LocalEndPoint).ToString();
			this.RemoteAddress = ((IPEndPoint)_socket.RemoteEndPoint).ToString();
			this.BeginReceive();
		}

		/// <summary>
		/// Connects to host without blocking the thread.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		public void ConnectAsync(string host, int port)
		{
			var remoteEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
			this.ConnectAsync(remoteEndPoint);
		}

		/// <summary>
		/// Connects to host without blocking the thread.
		/// </summary>
		/// <param name="remoteEndPoint"></param>
		public void ConnectAsync(IPEndPoint remoteEndPoint)
		{
			if (_socket != null)
				this.Disconnect();

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			_socket.BeginConnect(remoteEndPoint, this.OnConnect, null);

			this.Status = ClientStatus.Connecting;
		}

		/// <summary>
		/// Called when connection was established or failed to be.
		/// </summary>
		/// <param name="result"></param>
		private void OnConnect(IAsyncResult result)
		{
			var success = false;

			try
			{
				_socket.EndConnect(result);

				this.LocalAddress = ((IPEndPoint)_socket.LocalEndPoint).ToString();
				this.RemoteAddress = ((IPEndPoint)_socket.RemoteEndPoint).ToString();
				this.BeginReceive();

				success = true;
			}
			catch (SocketException ex)
			{
				this.LastError = string.Format("{0}", ex.SocketErrorCode, ex.Message);
				this.LastException = ex;
				ConnectionFailed?.Invoke(ex);
			}
			catch (Exception ex)
			{
				this.LastError = ex.Message;
				this.LastException = ex;
                ConnectionFailed?.Invoke(ex);
			}

			if (!success)
			{
				this.Status = ClientStatus.NotConnected;
			}
			else
			{
				this.Status = ClientStatus.Connected;
				Connected?.Invoke(this);
			}
		}

		/// <summary>
		/// Disconnects client.
		/// </summary>
		public void Disconnect()
		{
			if (this.Status == ClientStatus.NotConnected)
				return;

			this.Status = ClientStatus.NotConnected;

			try { _socket.Shutdown(SocketShutdown.Both); }
			catch { }
			try { _socket.Close(); }
			catch { }

			this.OnDisconnect(ConnectionCloseType.Closed);
		}

		/// <summary>
		/// Called when the client is disconnected in some way, raises
		/// Closed event.
		/// </summary>
		/// <param name="type"></param>
		protected virtual void OnDisconnect(ConnectionCloseType type)
		{
			this.Disconnected?.Invoke(this, type);
		}

		/// <summary>
		/// Starts receiving data.
		/// </summary>
		private void BeginReceive()
		{
			_socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, this.OnReceive, _socket);
		}

		/// <summary>
		/// Called on incoming data.
		/// </summary>
		/// <param name="result"></param>
		private void OnReceive(IAsyncResult result)
		{
			try
			{
				// As a new socket is created for each new connection,
				// the one that was used for the BeginReceive is passed
				// via the result. If the socket is one that was
				// disconnected ObjectDisposedException will be thrown,
				// which is the end of that socket.

				var socket = (Socket)result.AsyncState;
				var length = socket.EndReceive(result);

				if (length == 0)
				{
					this.Status = ClientStatus.NotConnected;
					this.OnDisconnect(ConnectionCloseType.Disconnected);

					return;
				}

				this.ReceiveData(_buffer, length);

				this.BeginReceive();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException ex)
			{
				this.LastError = string.Format("{0}", ex.SocketErrorCode, ex.Message);
				this.LastException = ex;

				this.Status = ClientStatus.NotConnected;
				this.OnDisconnect(ConnectionCloseType.Lost);
			}
			catch (Exception ex)
			{
				this.LastError = ex.Message;
				this.LastException = ex;

				this.OnReceiveException(ex);
				this.Disconnect();
			}
		}

		/// <summary>
		/// Called if an exception occurs while receiving data,
		/// raises ReceiveException event.
		/// </summary>
		protected virtual void OnReceiveException(Exception ex)
		{
			this.ReceiveException?.Invoke(this, ex);
		}

		/// <summary>
		/// Called on incoming data.
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="length"></param>
		protected abstract void ReceiveData(byte[] buffer, int length);

		/// <summary>
		/// Sends data via socket.
		/// </summary>
		/// <param name="data"></param>
		public virtual void Send(byte[] data)
		{
			if (this.Status == ClientStatus.Connected)
				_socket.Send(data);
		}
	}

	/// <summary>
	/// A client's connection status.
	/// </summary>
	public enum ClientStatus
	{
		/// <summary>
		/// Client is not connected.
		/// </summary>
		NotConnected,

		/// <summary>
		/// Client is connecting asynchronously.
		/// </summary>
		Connecting,

		/// <summary>
		/// Cliet is connected.
		/// </summary>
		Connected,
	}

	/// <summary>
	/// The way a connection was closed.
	/// </summary>
	public enum ConnectionCloseType
	{
		/// <summary>
		/// The connection was closed by the host.
		/// </summary>
		Closed,

		/// <summary>
		/// The connection was closed by the client.
		/// </summary>
		Disconnected,

		/// <summary>
		/// The connection was lost unexpectedly.
		/// </summary>
		Lost,
	}
}

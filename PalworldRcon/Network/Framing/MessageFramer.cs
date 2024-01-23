namespace PalworldRcon.Network.Framing
{
	/// <summary>
	/// Interface for network message framers.
	/// </summary>
	public interface IMessageFramer
	{
		/// <summary>
		/// Framges the given message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		byte[] Frame(byte[] message);

		/// <summary>
		/// Handles the given data.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="length"></param>
		void ReceiveData(byte[] data, int length);
	}
}

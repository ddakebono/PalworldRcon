using System;

namespace PalworldRcon.Logging
{
	/// <summary>
	/// Specifies a log message's type.
	/// </summary>
	[Flags]
	public enum LogLevel
	{
		/// <summary>
		/// Informational message.
		/// </summary>
		Info = 0x0001,

		/// <summary>
		/// A warning that something went wrong.
		/// </summary>
		Warning = 0x0002,

		/// <summary>
		/// A error message.
		/// </summary>
		Error = 0x0004,

		/// <summary>
		/// A debug log.
		/// </summary>
		Debug = 0x0008,

		/// <summary>
		/// A status information, comparable to Info.
		/// </summary>
		Status = 0x0010,

		/// <summary>
		/// No/all levels, used to filter log levels.
		/// </summary>
		None = 0x7FFF,
	}
}

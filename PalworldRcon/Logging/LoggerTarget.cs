namespace PalworldRcon.Logging
{
	/// <summary>
	/// A target for a Logger, that gets send all log messages passed to
	/// the logger.
	/// </summary>
	public abstract class LoggerTarget
	{
		/// <summary>
		/// LogLevels to hide.
		/// </summary>
		public LogLevel Filter { get; set; }

		/// <summary>
		/// The logger this target belongs to, set automatically.
		/// </summary>
		public Logger Logger { get; internal set; }

		/// <summary>
		/// Called when logger has something to log.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message"></param>
		/// <param name="messageRaw"></param>
		/// <param name="messageClean"></param>
		public abstract void Write(LogLevel level, string message, string messageRaw, string messageClean);

		/// <summary>
		/// Format for the log message.
		/// </summary>
		/// <example>
		/// return "[{0}] - {1}";
		/// 
		/// {0}: Log level
		/// {1}: Log message
		/// </example>
		/// <param name="level"></param>
		/// <returns></returns>
		public abstract string GetFormat(LogLevel level);

		/// <summary>
		/// Returns true if given level is being filtered on this target.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public bool Filtered(LogLevel level)
		{
			return ((this.Filter & level) != 0);
		}
	}
}

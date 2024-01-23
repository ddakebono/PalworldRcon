using PalworldRcon.Logging.Targets;

namespace PalworldRcon.Logging
{
	/// <summary>
	/// Logs messages to command line and file.
	/// </summary>
	public static class Log
	{
		private static Logger _logger = Logger.Get();

		static Log()
		{
			_logger.AddTarget(new ConsoleTarget());
			_logger.AddTarget(new FileTarget("logs"));
		}

		/// <summary>
		/// Logs an info message.
		/// </summary>
		/// <param name="value"></param>
		public static void Info(string value) { _logger.Info(value); }

		/// <summary>
		/// Logs an info message.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Info(string format, params object[] args) { _logger.Info(format, args); }

		/// <summary>
		/// Logs an info message.
		/// </summary>
		/// <param name="obj"></param>
		public static void Info(object obj) { _logger.Info(obj); }

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="value"></param>
		public static void Warning(string value) { _logger.Warning(value); }

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Warning(string format, params object[] args) { _logger.Warning(format, args); }

		/// <summary>
		/// Logs a warning message.
		/// </summary>
		/// <param name="obj"></param>
		public static void Warning(object obj) { _logger.Warning(obj); }

		/// <summary>
		/// Logs an error message.
		/// </summary>
		/// <param name="value"></param>
		public static void Error(string value) { _logger.Error(value); }

		/// <summary>
		/// Logs an error message.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Error(string format, params object[] args) { _logger.Error(format, args); }

		/// <summary>
		/// Logs an error message.
		/// </summary>
		/// <param name="obj"></param>
		public static void Error(object obj) { _logger.Error(obj); }

		/// <summary>
		/// Logs a debug message.
		/// </summary>
		/// <param name="value"></param>
		public static void Debug(string value) { _logger.Debug(value); }

		/// <summary>
		/// Logs a debug message.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Debug(string format, params object[] args) { _logger.Debug(format, args); }

		/// <summary>
		/// Logs a debug message.
		/// </summary>
		/// <param name="obj"></param>
		public static void Debug(object obj) { _logger.Debug(obj); }

		/// <summary>
		/// Logs a status message.
		/// </summary>
		/// <param name="value"></param>
		public static void Status(string value) { _logger.Status(value); }

		/// <summary>
		/// Logs a status message.
		/// </summary>
		/// <param name="format"></param>
		/// <param name="args"></param>
		public static void Status(string format, params object[] args) { _logger.Status(format, args); }

		/// <summary>
		/// Logs a status message.
		/// </summary>
		/// <param name="obj"></param>
		public static void Status(object obj) { _logger.Status(obj); }

		/// <summary>
		/// Sets levels that should not be logged.
		/// </summary>
		/// <param name="levels"></param>
		public static void SetFilter(LogLevel levels)
		{
			var targets = _logger.GetTargets();

			foreach (var target in targets)
				target.Filter = levels;
		}
	}
}

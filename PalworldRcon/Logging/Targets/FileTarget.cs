using System;
using System.IO;
using System.Linq;

namespace PalworldRcon.Logging.Targets
{
	/// <summary>
	/// Logger target logging to a text file.
	/// </summary>
	public class FileTarget : LoggerTarget
	{
		/// <summary>
		/// The folder the log file is in.
		/// </summary>
		public string FolderPath { get; private set; }

		/// <summary>
		/// The path to the log file.
		/// </summary>
		public string FilePath { get; private set; }

		private DateTime _logStartTime;

		/// <summary>
		/// Creates new instance, with the file going into the given folder.
		/// </summary>
		/// <param name="folderPath"></param>
		public FileTarget(string folderPath = "")
		{
			this.FolderPath = folderPath;
		}

		/// <summary>
		/// Writes clean message to the log file, prepending it with the
		/// time and date the message was written at.
		/// </summary>
		/// <param name="level"></param>
		/// <param name="message"></param>
		/// <param name="messageRaw"></param>
		/// <param name="messageClean"></param>
		public override void Write(LogLevel level, string message, string messageRaw, string messageClean)
		{
			var time = DateTime.Now;

			//Every 24 hours generate a new log file
			if (this.FilePath != null && time.Subtract(this._logStartTime).TotalHours >= 24)
				this.FilePath = null;
			
			if (this.FilePath == null)
			{
				this._logStartTime = time;

				this.FilePath = Path.Combine(this.FolderPath, $"{this.Logger.Name}_{this._logStartTime:MM.dd.yyyy-HH.mm}_.txt");

				if (File.Exists(this.FilePath))
					File.Delete(this.FilePath);

				if (!Directory.Exists(this.FolderPath))
				{
					Directory.CreateDirectory(this.FolderPath);
				}
				else
				{
					//Check for log clean
					var logs = new DirectoryInfo(this.FolderPath).GetFiles();
					
					if (logs.Length > 15)
					{
						foreach (var file in logs.OrderByDescending(x => x.LastWriteTime).Skip(15))
						{
							file.Delete();
						}
					}
				}
			}

			messageClean = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {messageClean}";

			File.AppendAllText(this.FilePath, messageClean);
		}

		/// <summary>
		/// Returns the format for the raw log message.
		/// </summary>
		/// <param name="level"></param>
		/// <returns></returns>
		public override string GetFormat(LogLevel level)
		{
			return "[{0}] - {1}";
		}
	}
}

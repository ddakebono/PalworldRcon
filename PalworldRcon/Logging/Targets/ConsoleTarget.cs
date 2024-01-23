using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PalworldRcon.Logging.Targets
{
    public class ConsoleTarget : LoggerTarget
    {
        public static ConsoleTarget Instance;

        public ObservableCollection<string> OutputText { get; } = new();

        public ConsoleTarget()
        {
            Instance = this;
        }

        /// <summary>
        /// Writes message to Console standard output.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        /// <param name="messageRaw"></param>
        /// <param name="messageClean"></param>
        public override void Write(LogLevel level, string message, string messageRaw, string messageClean)
        {
            messageRaw = messageRaw.TrimEnd(Environment.NewLine.ToCharArray());

            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                OutputText.Add(messageRaw);
            });
        }

        /// <summary>
        /// Returns color coded formats, based on log level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public override string GetFormat(LogLevel level)
        {
            return "[{0}] - {1}";
        }
    }
}
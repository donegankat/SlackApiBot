using SlackApiBot.Infrastructure.Exceptions;
using System.Text;

namespace SlackApiBot.Infrastructure.Logging
{
    public static class Logger
    {
        private static readonly string _outputLogFileName = $"SlackApiBot {DateTime.Now.ToString("yyyy-MM-dd")}.txt";
        private static StringBuilder _logText = new StringBuilder();
        private static Settings.Settings _settings = new Settings.Settings();

        public static void InitializeLog(Settings.Settings settings)
        {
            _settings = settings;

            if (_settings == null)
                throw ConfigurationException.MissingSettingsException();

            if (_settings.ShouldOutputLogFile && string.IsNullOrWhiteSpace(_settings.LogFileOutputDirectory))
                throw new ConfigurationException("Settings were configured to output a log file, but no directory was provided.", nameof(_settings.LogFileOutputDirectory));

            _logText = new StringBuilder();
            _logText.AppendLine();
            _logText.AppendLine("----------------------------------");
            _logText.AppendLine($"Begin Time: {DateTime.Now.ToString("hh:mm:ss")}");
            _logText.AppendLine();
        }

        public static void Log(string text, LogType type = LogType.Basic)
        {
            switch (type)
            {
                case LogType.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogType.Info:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogType.Basic:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogType.Debug:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
            }

            if (type != LogType.Debug || _settings.EnableDebugLogging)
                // Console output complains about bullet chars, so get rid of them just for the console.
                Console.WriteLine(text?.Replace("•", "-"));

            _logText.AppendLine(text);

            // Set the color back to normal.
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        /// <summary>
        /// Inserts a line break into the log & console output.
        /// </summary>
        public static void LineBreak()
        {
            Console.WriteLine();
            _logText.AppendLine();
        }

        public static string UserInput()
        {
            LineBreak();
            Console.ForegroundColor = ConsoleColor.Green;

            var userInput = Console.ReadLine();
            _logText.AppendLine(userInput);

            return userInput ?? string.Empty;
        }

        public static void Info(string text)
        {
            Log(text, LogType.Info);
        }

        public static void Success(string text)
        {
            Log(text, LogType.Success);
        }

        public static void Warning(string text)
        {
            Log(text, LogType.Warning);
        }

        public static void Error(string text)
        {
            Log(text, LogType.Error);
        }

        public static void Error(Exception ex)
        {
            Log(ex.ToString(), LogType.Error);
        }

        public static void Debug(string text)
        {
            Log(text, LogType.Debug);
        }

        /// <summary>
        /// Writes the full log to a file if the config file was configured to do so.
        /// </summary>
        public static void WriteLogFile()
        {
            try
            {
                _logText.AppendLine();
                _logText.AppendLine($"End Time: {DateTime.Now.ToString("hh:mm:ss")}");
                _logText.AppendLine();

                if (_settings.ShouldOutputLogFile && !string.IsNullOrWhiteSpace(_settings.LogFileOutputDirectory))
                {
                    Directory.CreateDirectory(_settings.LogFileOutputDirectory);

                    File.AppendAllText($"{_settings.LogFileOutputDirectory}/{_outputLogFileName}", _logText.ToString());
                }
            }
            catch (Exception ex)
            {
                // Write the error to the console but there's nothing we can really do here so just swallow it.
                Error($"Failed to save log file. {ex.Message}\n\n{ex.StackTrace}");
            }
        }
    }
}

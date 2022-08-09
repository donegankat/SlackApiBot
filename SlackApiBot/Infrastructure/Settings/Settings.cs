namespace SlackApiBot.Infrastructure.Settings
{
    public class Settings
    {
        /// <summary>
        /// The API authentication token needed to communicate with the Slack API.
        /// </summary>
        public string SlackApiToken { get; set; } = string.Empty;

        /// <summary>
        /// Whether or not the debug level of logging is enabled, which logs more info than normal.
        /// </summary>
        public bool EnableDebugLogging { get; set; } = false;

        /// <summary>
        /// Whether or not the app should write a log file to disk.
        /// If true, the log file is written to the configured LogFileOutputDirectory.
        /// </summary>
        public bool ShouldOutputLogFile { get; set; } = false;

        /// <summary>
        /// The directory path where the log file should be output to if ShouldOutputLogFile
        /// is true.
        /// </summary>
        public string? LogFileOutputDirectory { get; set; } = "C:\\Temp";

        /// <summary>
        /// Settings specific to the channel announcement functionality.
        /// </summary>
        public ChannelAnnouncerSettings ChannelAnnouncer { get; set; } = new ChannelAnnouncerSettings();
    }
}

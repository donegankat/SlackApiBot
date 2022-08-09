namespace SlackApiBot.Infrastructure.Exceptions
{
    /// <summary>
    /// Exception that's thrown when there's a misconfiguration in the app's settings.
    /// </summary>
    public class ConfigurationException : Exception
    {
        /// <summary>
        /// The name of the setting that caused the error, if it's known.
        /// </summary>
        public string? SettingName { get; private set; }

        public ConfigurationException(string message) : base(message)
        {
        }

        public ConfigurationException(string message, string settingName) : this(message)
        {
            SettingName = settingName;
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(SettingName))
                return $"{base.ToString()} Setting Name: {SettingName}";

            return base.ToString();
        }

        /// <summary>
        /// Returns an exception that indicates that the settings file was missing or couldn't be loaded.
        /// </summary>
        /// <returns></returns>
        public static ConfigurationException MissingSettingsException()
        {
            return new ConfigurationException("Settings file was missing or invalid.");
        }
    }
}

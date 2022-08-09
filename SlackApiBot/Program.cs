using SlackApiBot.Slack;
using Microsoft.Extensions.Configuration;
using SlackApiBot.Infrastructure.Settings;
using SlackApiBot.Infrastructure.Logging;
using SlackApiBot.Infrastructure.Exceptions;

namespace SlackApiBot
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var isAutomaticRun = args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) && "autorun".Equals(args[0], StringComparison.InvariantCultureIgnoreCase);

			try
			{
				var settings = LoadAppSettings();

				if (settings == null)
					throw ConfigurationException.MissingSettingsException();
				if (string.IsNullOrWhiteSpace(settings.SlackApiToken))
					throw new ConfigurationException("Missing the Slack API token in the settings file.", nameof(settings.SlackApiToken));

				Logger.InitializeLog(settings);

				await RunSlackBot(settings);
			}
			catch (Exception ex)
			{
				// Set the program's exit code to -1 so that the pipeline is able to recognize that the
				// program failed to run successfully.
				Environment.ExitCode = -1;

				Logger.Error($"ERROR: {ex}");
			}

			// If configured to do so, write the log output to a file.
			Logger.WriteLogFile();
			Logger.LineBreak();

			// If we're running automatically, exit automatically. Otherwise, wait for user input.
			if (isAutomaticRun)
				return;

			Logger.Info("Press any key to exit.");
			Console.ReadKey();
		}

		/// <summary>
		/// Loads the settings from the config file.
		/// </summary>
		/// <returns></returns>
		private static Settings LoadAppSettings()
		{
			var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			IConfigurationBuilder builder;

			if (string.IsNullOrWhiteSpace(environment))
            {
				// No environment means we should just load the local, non-environment-specific appSettings.
				builder = new ConfigurationBuilder()
				.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
				.AddEnvironmentVariables();
			}
			else
            {
				// If we have an environment, we should load the normal appSettings and override them with
				// environment-specific settings like the ones from appSettings.{environment name}.json.
				builder = new ConfigurationBuilder()
				.AddJsonFile("appSettings.json", optional: false, reloadOnChange: true)
				.AddJsonFile($"appsettings.{environment}.json", true, true)

				// This next line allows us to dynamically pass in overrides for any of the appSettings by
				// executing the program with defined environment variables. This can be done a number of
				// ways, but the easiest is probably defining the variables in PowerShell and then running
				// the SlackApiBot.exe.
				// Example PowerShell commands which override some of the settings:
				//    $env:channelAnnouncer__daysToLookBackForNewChannels=9
				//    $env:channelAnnouncer__announcementChannelName="testing"
				//    .\SlackApiBot.exe
				// More info on environment variables here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-6.0#non-prefixed-environment-variables
				.AddEnvironmentVariables();
			}

			var configuration = builder.Build();

			var settings = new Settings();
			configuration.Bind(settings);

			return settings;
		}

		/// <summary>
		/// Performs all of the main, Slack-related functions.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns></returns>
		public static async Task RunSlackBot(Settings settings)
        {
			var channelAnnouncer = new ChannelAnnouncer(settings);
			
			// Announce any newly created channels if the configuration says to do so.
			if (settings.ChannelAnnouncer.Enabled)
				await channelAnnouncer.AnnnounceNewChannels();
			else
				Logger.Info("Channel announcer was not enabled. Skipping announcement.");

			// Someday, put additional stuff here if we ever make this do more stuff.
		}
	}
}
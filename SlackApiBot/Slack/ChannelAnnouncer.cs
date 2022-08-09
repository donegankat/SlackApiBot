using SlackAPI;
using SlackApiBot.Infrastructure.Exceptions;
using SlackApiBot.Infrastructure.Logging;
using SlackApiBot.Infrastructure.Settings;
using System.Text;

namespace SlackApiBot.Slack
{
    /// <summary>
    /// Queries the Slack API for the list of all channels, and if any new channels have been created
    /// in the desired window, posts a message to a channel announcing the new channels.
    /// </summary>
    public class ChannelAnnouncer
    {
        private readonly Settings _settings;
        private readonly SlackTaskClient _slackClient;

        public ChannelAnnouncer(Settings settings)
        {
            _settings = settings;
            _slackClient = new SlackTaskClient(_settings.SlackApiToken);

            if (_settings == null)
                throw ConfigurationException.MissingSettingsException();
            if (_settings.ChannelAnnouncer == null)
                throw new ConfigurationException("No ChannelAnnouncer settings were provided.", nameof(settings.ChannelAnnouncer));
            if (string.IsNullOrWhiteSpace(_settings.ChannelAnnouncer.AnnouncementChannelName))
                throw new ConfigurationException("No AnnouncementChannelName was provided.", nameof(settings.ChannelAnnouncer.AnnouncementChannelName));

            // Just in case someone forgot to begin the announcement channel name with a #, fix that here.
            if (!settings.ChannelAnnouncer.AnnouncementChannelName.StartsWith("#"))
                settings.ChannelAnnouncer.AnnouncementChannelName = $"#{settings.ChannelAnnouncer.AnnouncementChannelName}";

            // Log some of the important settings for debug purposes.
            Logger.Debug($"Channel Announcer Days to Look Back: {settings.ChannelAnnouncer.DaysToLookBackForNewChannels}");
            Logger.Debug($"Channel Announcer Announcement Channel Name: {settings.ChannelAnnouncer.AnnouncementChannelName}");

            var channelsToExcludeLogString = settings.ChannelAnnouncer.ChannelsToExclude == null || !settings.ChannelAnnouncer.ChannelsToExclude.Any()
                ? "none"
                : string.Join(", ", settings.ChannelAnnouncer.ChannelsToExclude);
            Logger.Debug($"Channel Announcer Channels to Exclude: {channelsToExcludeLogString}");
            var channelPrefixesToExcludeLogString = settings.ChannelAnnouncer.ChannelPrefixesToExclude == null || !settings.ChannelAnnouncer.ChannelPrefixesToExclude.Any()
                ? "none"
                : string.Join(", ", settings.ChannelAnnouncer.ChannelPrefixesToExclude);
            Logger.Debug($"Channel Announcer Channel Prefixes to Exclude: {channelPrefixesToExcludeLogString}");
        }

        /// <summary>
        /// Retrieves all Slack channels, builds an announcement message about any newly created channels,
        /// and then posts an announcement to a Slack channel.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task AnnnounceNewChannels()
        {
            var allChannels = await GetAllChannels();
            var message = BuildAnnouncementMessage(allChannels);

            Logger.Info($"Posting to: ${_settings.ChannelAnnouncer.AnnouncementChannelName}.");
            Logger.Debug($"Message: ${message}");

            var response = await _slackClient.PostMessageAsync(
                _settings.ChannelAnnouncer.AnnouncementChannelName,
                "",
                null,
                null,
                false,
                new IBlock[]
                {
                    new SectionBlock
                    {
                        text = new Text
                        {
                            type = TextTypes.Markdown,
                            text = message
                        }
                    }
                }
            );

            if (response.ok)
                Logger.Success("Success");
            else
                throw new SlackApiException("Slack request received an error response when attempting to post new channel announcement.", response.error, response);
        }

        /// <summary>
        /// Gets the full list of channels from the Slack API.
        /// </summary>
        /// <returns></returns>
        private async Task<List<Channel>> GetAllChannels()
        {
            var nextPageCursor = string.Empty;
            var allChannels = new List<Channel>();

            // Fetch all channels by continuing to get each available page of channels until there are no
            // longer any more pages to fetch.
            while (true)
            {
                // Always be sure to only ever grab non-archived, public channels. The bot integration doesn't
                // have rights to grab private channels anyway, but just in case they ever get accidentally
                // granted, play it safe.
                // Slack docs for this API call: https://api.slack.com/methods/conversations.list
                var response = await _slackClient.GetConversationsListAsync(
                    nextPageCursor,                     // Page to grab
                    true,                               // Exclude archived channels
                    100,                                // Max page size
                    new string[] { "public_channel" }   // Channel types to grab. The default should already "public_channel" only, but to be safe we hardcode it again.
                );

                if (response == null) break;

                if (!response.ok)
                    throw new SlackApiException($"Failed to fetch channels. Current page cursor: {nextPageCursor}", response.error, response);

                nextPageCursor = response.response_metadata?.next_cursor;

                allChannels.AddRange(response.channels);

                if (string.IsNullOrWhiteSpace(nextPageCursor)) break;
            }

            return allChannels;
        }

        /// <summary>
        /// Builds an announcement message which lists all of the recently created channels.
        /// </summary>
        /// <param name="allChannels"></param>
        /// <returns></returns>
        public string BuildAnnouncementMessage(List<Channel> allChannels)
        {
            var message = new StringBuilder();

            if (allChannels != null && allChannels.Any())
            {
                var newlyCreatedChannels = GetNewlyCreatedChannelsList(allChannels);

                // If we have any channels left in the list that we need to announce, build a message.
                if (newlyCreatedChannels != null && newlyCreatedChannels.Any())
                {
                    var wereText = newlyCreatedChannels.Count() > 1 ? "were" : "was";
                    var channelsText = newlyCreatedChannels.Count() > 1 ? "channels" : "channel";
                    var randomEmoji = GetRandomAnnoucementEmoji();

                    message.Append($"There {wereText} {newlyCreatedChannels.Count()} new {channelsText} created recently! :{randomEmoji}:");

                    foreach (var channel in newlyCreatedChannels.Where(channel => channel != null))
                    {
                        message.Append($"\n• #{channel.name} ({channel.num_members} members)");

                        if (!string.IsNullOrWhiteSpace(channel.topic?.value))
                            message.Append($" - {channel.topic.value}");
                        else if (!string.IsNullOrWhiteSpace(channel.purpose?.value))
                            message.Append($" - {channel.purpose.value}");
                    }
                }
            }

            if (message.Length <= 0)
                message.Append("No new channels were created recently. See you again next week!");

            return message.ToString();
        }

        /// <summary>
        /// Returns the list of channels which were created within the configured time period and which
        /// aren't in the list of excluded channels and don't begin with any of the excluded prefixes.
        /// </summary>
        /// <param name="allChannels"></param>
        /// <returns></returns>
        private IEnumerable<Channel> GetNewlyCreatedChannelsList(List<Channel> allChannels)
        {
            int daysToLookBack = 7;

            if (_settings.ChannelAnnouncer.DaysToLookBackForNewChannels > 0)
                daysToLookBack = _settings.ChannelAnnouncer.DaysToLookBackForNewChannels;

            var newlyCreatedChannels = allChannels.Where(channel => channel.created >= DateTime.Now.AddDays(-daysToLookBack));

            if (newlyCreatedChannels != null && newlyCreatedChannels.Any() && _settings.ChannelAnnouncer.ChannelPrefixesToExclude != null && _settings.ChannelAnnouncer.ChannelPrefixesToExclude.Any())
            {
                // If we were given a list of channel prefixes to exclude, remove them from the list.
                foreach (var prefixToExclude in _settings.ChannelAnnouncer.ChannelPrefixesToExclude)
                {
                    newlyCreatedChannels = newlyCreatedChannels.Where(channel => channel.name?.IndexOf(prefixToExclude, StringComparison.InvariantCultureIgnoreCase) != 0);
                }
            }

            if (newlyCreatedChannels != null && newlyCreatedChannels.Any() && _settings.ChannelAnnouncer.ChannelsToExclude != null && _settings.ChannelAnnouncer.ChannelsToExclude.Any())
            {
                // If we were given a list of specific channels to exclude, remove them from the list.
                foreach (var channelToExclude in _settings.ChannelAnnouncer.ChannelsToExclude)
                {
                    newlyCreatedChannels = newlyCreatedChannels.Where(channel => !channelToExclude.Equals(channel.name, StringComparison.InvariantCultureIgnoreCase));
                }
            }

            return newlyCreatedChannels ?? new List<Channel>();
        }

        /// <summary>
        /// Returns a random emoji from the configuration list.
        /// </summary>
        /// <returns></returns>
        private string GetRandomAnnoucementEmoji()
        {
            var emojisToChooseFrom = new List<string>();

            foreach (var emoji in _settings.ChannelAnnouncer.AnnouncementEmojis)
            {
                // Make sure someone didn't define the emoji surrounded by colons already. We add those later.
                var cleanEmoji = emoji.Replace(":", "").Trim();

                if (!string.IsNullOrWhiteSpace(cleanEmoji))
                    emojisToChooseFrom.Add(cleanEmoji);
            }

            if (emojisToChooseFrom.Any())
            {
                var randomEmojiIndex = new Random().Next(emojisToChooseFrom.Count);
                var randomEmoji = emojisToChooseFrom[randomEmojiIndex];

                Logger.Debug($"Randomly selected the :{randomEmoji}: emoji.");
                return randomEmoji;
            }

            Logger.Warning("No valid emojis were found. Falling back to a hard-coded default.");
            return "tada";
        }
    }
}

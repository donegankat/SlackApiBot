namespace SlackApiBot.Infrastructure.Settings
{
    public class ChannelAnnouncerSettings
    {
        /// <summary>
        /// Whether or not the ChannelAnnouncer should execute.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The number of days to look back for newly created channels.
        /// </summary>
        public int DaysToLookBackForNewChannels { get; set; } = 7;

        /// <summary>
        /// The name of the channel to post new channel announcements to.
        /// </summary>
        public string AnnouncementChannelName { get; set; } = string.Empty;

        /// <summary>
        /// The optional list of channel names to exclude from announcements.
        /// </summary>
        public string[]? ChannelsToExclude { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The optional list of channel prefixes to exclude from announcements.
        /// Channels which begin with these prefixes will not be announced.
        /// </summary>
        public string[]? ChannelPrefixesToExclude { get; set; } = Array.Empty<string>();

        /// <summary>
        /// A list of emojis that the announcer will randomly choose from when posting
        /// an announcement.
        /// </summary>
        public string[] AnnouncementEmojis { get; set; } = Array.Empty<string>();
    }
}

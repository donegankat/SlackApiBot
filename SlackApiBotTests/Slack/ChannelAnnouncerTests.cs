using SlackApiBot.Infrastructure.Logging;
using SlackApiBot.Infrastructure.Settings;
using SlackApiBot.Slack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlackApiBotTests.Slack
{
    [TestClass]
    public class ChannelAnnouncerTests
    {
        private Settings _settings = new Settings();

        [TestInitialize]
        public void Initialize()
        {
            _settings = new Settings
            {
                SlackApiToken = "unit-test",
                ShouldOutputLogFile = false,
                EnableDebugLogging = false,
                ChannelAnnouncer = new ChannelAnnouncerSettings
                {
                    AnnouncementChannelName = "#unit-test",
                    DaysToLookBackForNewChannels = 7,
                    AnnouncementEmojis = new[] {
                        "tada",
                        "celebrate",
                        "party_blob",
                        "promeo",
                        "bromeo",
                        "yeahteddy"
                    }
                }
            };

            // Initialize the logger so that normal logging calls always succeed, even if we don't
            // ever do anything with the logs for unit testing.
            Logger.InitializeLog(_settings);
        }

        [TestMethod]
        public void BuildAnnouncementMessageBuildsEmptyMessageWhenNoNewChannels()
        {
            var channelAnnouncer = new ChannelAnnouncer(_settings);
            var channelList = new List<SlackAPI.Channel>
            {
                new SlackAPI.Channel
                {
                    name = "test1",
                    created = DateTime.Now.AddDays(-_settings.ChannelAnnouncer.DaysToLookBackForNewChannels - 1)
                },
                new SlackAPI.Channel
                {
                    name = "test2",
                    created = DateTime.Now.AddDays(-100)
                },
                new SlackAPI.Channel
                {
                    name = "test3",
                    created = DateTime.Now.AddDays(-_settings.ChannelAnnouncer.DaysToLookBackForNewChannels).AddMinutes(-1)
                },
                new SlackAPI.Channel
                {
                    name = "test4"
                }
            };

            var announcementMessage = channelAnnouncer.BuildAnnouncementMessage(channelList);

            Assert.AreEqual("No new channels were created recently. See you again next week!", announcementMessage);
        }

        [TestMethod]
        public void BuildAnnouncementMessagePluralizesCorrectly()
        {
            var channelAnnouncer = new ChannelAnnouncer(_settings);
            var channelList = new List<SlackAPI.Channel>
            {
                new SlackAPI.Channel
                {
                    name = "test1",
                    created = DateTime.Now
                }
            };

            var announcementMessageSingle = channelAnnouncer.BuildAnnouncementMessage(channelList);

            Assert.IsTrue(announcementMessageSingle.StartsWith("There was 1 new channel created"), $"Expected singular message but received: {announcementMessageSingle}");

            channelList.Add(new SlackAPI.Channel
            {
                name = "test2",
                created = DateTime.Now
            });

            var announcementMessagePlural = channelAnnouncer.BuildAnnouncementMessage(channelList);

            Assert.IsTrue(announcementMessagePlural.StartsWith("There were 2 new channels created"), $"Expected plural message but received: {announcementMessagePlural}");
        }

        [TestMethod]
        public void BuildAnnouncementMessageSelectsRandomEmojis()
        {
            var channelAnnouncer = new ChannelAnnouncer(_settings);
            var channelList = new List<SlackAPI.Channel>
            {
                new SlackAPI.Channel
                {
                    name = "test1",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "test2",
                    created = DateTime.Now
                }
            };

            var seenEmojis = new List<string>();

            for (var i = 0; i < 100; i++)
            {
                var announcementMessage = channelAnnouncer.BuildAnnouncementMessage(channelList);
                var emojiIndexStart = announcementMessage.IndexOf(":");

                Assert.IsTrue(emojiIndexStart > -1, $"Did not find an emoji in the announcement message: {announcementMessage}");

                var emojiIndexEnd = announcementMessage.IndexOf(":", emojiIndexStart + 1);

                Assert.IsTrue(emojiIndexEnd > -1, $"Did not find a closing colon for the emoji in the announcement message: {announcementMessage}");
                
                var emoji = announcementMessage.Substring(emojiIndexStart + 1, (emojiIndexEnd - emojiIndexStart - 1));

                Assert.IsFalse(string.IsNullOrWhiteSpace(emoji), $"Did not find valid emoji in the announcement message: {announcementMessage}");

                if (!seenEmojis.Contains(emoji))
                    seenEmojis.Add(emoji);
            }

            // Technically because it relies on randomness, this COULD be flaky, but the chances of that
            // happening are basically zero with 100 attempts to randomize.
            Assert.IsTrue(seenEmojis.Count > 1, $"Did not receive sufficient evidence of emoji randomization after 100 tries. Seen emojis: {string.Join(", ", seenEmojis)}");
        }

        [TestMethod]
        public void BuildAnnouncementMessageExcludesConfiguredChannels()
        {
            _settings.ChannelAnnouncer.ChannelsToExclude = new string[]
            {
                "excluded-1",
                "ExClUdEd2",
                "EXCLUDED_3",
                "-excluded-4" // Dash channel variation
            };

            var channelAnnouncer = new ChannelAnnouncer(_settings);
            var channelList = new List<SlackAPI.Channel>
            {
                new SlackAPI.Channel
                {
                    name = "test1",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded-1",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded2",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "test2",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded_3",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "-excluded-4",
                    created = DateTime.Now
                }
            };

            var announcementMessage = channelAnnouncer.BuildAnnouncementMessage(channelList);

            Assert.IsTrue(announcementMessage.StartsWith("There were 2 new channels created"), $"Expected 2 new channels but received: {announcementMessage}");
            Assert.IsTrue(announcementMessage.Contains("test1"), $"Expected test1 channel to be announced but received: {announcementMessage}");
            Assert.IsTrue(announcementMessage.Contains("test2"), $"Expected test2 channel to be announced but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("excluded-1"), $"Expected excluded-1 channel to be excluded but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("excluded2"), $"Expected excluded2 channel to be excluded but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("excluded_3"), $"Expected excluded_3 channel to be excluded but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("-excluded-4"), $"Expected -excluded-4 channel to be excluded but received: {announcementMessage}");
        }

        [TestMethod]
        public void BuildAnnouncementMessageExcludesConfiguredChannelPrefixes()
        {
            _settings.ChannelAnnouncer.ChannelPrefixesToExclude = new string[]
            {
                "excluded-1",
                "ExClUdEd2_",
                "EXCLUDED_3_",
                "-excluded-4-" // Dash channel variation
            };

            var channelAnnouncer = new ChannelAnnouncer(_settings);
            var channelList = new List<SlackAPI.Channel>
            {
                new SlackAPI.Channel
                {
                    name = "test1",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded-1-test",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded2_test",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "test2",
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "excluded_3test", // Should not be excluded because there's no dash or underscore.
                    created = DateTime.Now
                },
                new SlackAPI.Channel
                {
                    name = "-excluded-4-test",
                    created = DateTime.Now
                }
            };

            var announcementMessage = channelAnnouncer.BuildAnnouncementMessage(channelList);

            Assert.IsTrue(announcementMessage.StartsWith("There were 3 new channels created"), $"Expected 3 new channels but received: {announcementMessage}");
            Assert.IsTrue(announcementMessage.Contains("test1"), $"Expected test1 channel to be announced but received: {announcementMessage}");
            Assert.IsTrue(announcementMessage.Contains("test2"), $"Expected test2 channel to be announced but received: {announcementMessage}");
            Assert.IsTrue(announcementMessage.Contains("excluded_3test"), $"Expected excluded_3test channel to be announced but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("excluded-1-test"), $"Expected excluded-1-test channel to be excluded but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("excluded2_test"), $"Expected excluded2_test channel to be excluded but received: {announcementMessage}");
            Assert.IsFalse(announcementMessage.Contains("-excluded-4-test"), $"Expected -excluded-4-test channel to be excluded but received: {announcementMessage}");
        }
    }
}

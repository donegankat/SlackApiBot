# Introduction 
This tool can be used to integrate with a Slack workspace and perform tasks like posting announcement messages.

## Tasks that This Tool Can Perform
1. ChannelAnnouncer: Announces the recently created Slack channels by posting a message with the new channel list to an announcement channel
2. No other functionality yet, but maybe I'll add more stuff someday

# Getting Started
This tool is built on .NET 6, so you'll need to install the .NET 6 SDK in order to develop with it.

If all you're doing is running the .exe, you should be able to do that without any special installs as long as the copy of the .exe you're running was published as "self-contained". If it's not self-contained, then you'll need the .NET 6 runtime.

## Slack Integration
This tool uses the SlackAPI NuGet package to easily integrate with the Slack API. You can find the documentation for that library here: https://github.com/Inumedia/SlackAPI

### Setting Up a Connection to a Slack Workspace
To set up an integration between this app and a Slack workspace, someone must visit the [Slack API site](https://api.slack.com/apps).

1. In that site, create a new app and generate an API key
2. Grant the following OAuth permissions to the app:
   1. channels:read
   2. chat:write
   3. chat:write.public
3. Request access to the workspace on behalf of the app
4. Replace the `slackApiToken` in [appSettings.json](./SlackApiBot/appSettings.json) and [appSettings.Production.json](./SlackApiBot/appSettings.Production.json) with your new token

It may be extremely helpful for you to create a new free Slack workspace that you own so that you can test out the administrative side of developing, authorizing, and integrating your Slack app and figure out how everything works before trying to integrate your new app with your official Slack workspace.

### Helpful Slack Docs
Overview of the Slack API platform: https://api.slack.com/start/overview

Overview of how to create a Slack app: https://api.slack.com/start/overview#creating

Overview of Slack API integrations with the capability to send Slack messages: https://api.slack.com/messaging/sending

## Settings
Before you run the app, review the settings in [appSettings.json](./SlackApiBot/appSettings.json) and [appSettings.Production.json](./SlackApiBot/appSettings.Production.json).
- appSettings.json is used by default when the tool is run with no ASPNETCORE_ENVIRONMENT defined. If running from Visual Studio, you would do this by starting the tool using the "Development Environment" launch option.
- appSettings.Production.json is used when the tool is run with the ASPNETCORE_ENVIRONMENT of "Production". If running from Visual Studio, you would do this by starting the tool using the "Production Environment" launch option. Settings in appSettings.Production.json override any settings in appSettings.json.

For descriptions of each setting, see [Settings.cs](./SlackApiBot/Infrastructure/Settings/Settings.cs) and [ChannelAnnouncerSettings.cs](./SlackApiBot/Infrastructure/Settings/ChannelAnnouncerSettings.cs).

# Integrating with Azure DevOps Pipelines
Example YAML files are included in the repository which can be used in Azure DevOps pipelines for the following operations:
- [BuildAndTestSlackApiBot](./Pipeline-BuildAndTestSlackApiBot.yml): Builds the project, runs unit tests, and publishes the release version of the tool as a pipeline artifact which can then be downloaded and executed by other pipelines. Triggers on commits.
- [RunSlackApiBot](./Pipeline-RunSlackApiBot.yml): Downloads the released tool from the BuildAndTestSlackApiBot pipeline and executes it on a schedule.

The `pool` information must be changed in both YAML files before the pipelines can run. The `DownloadPipelineArtifact` task in the RunSlackApiBot pipeline must also be updated. See that task for more info.
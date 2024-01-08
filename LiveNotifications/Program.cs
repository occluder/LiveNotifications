using System.Text;
using System.Text.Json;
using MiniTwitch.PubSub;
using MiniTwitch.PubSub.Models;

ChannelTarget[] channels = JsonSerializer.Deserialize<ChannelTarget[]>(File.ReadAllText("config.json"))!;
Dictionary<long, ChannelTarget> channelsById = channels.ToDictionary(x => x.ChannelId);
PubSubClient ps = new(string.Empty);
HttpClient http = new();
await ps.ConnectAsync();
foreach (var channel in channels)
{
    await ps.ListenTo(Topics.VideoPlayback(channel.ChannelId));
    await ps.ListenTo(Topics.BroadcastSettingsUpdate(channel.ChannelId));
}

ps.OnStreamUp += async (id, up) =>
{
    string templateText = channelsById[id].LiveText;
    await http.PostAsync(channelsById[id].WebhookUrl, DiscordMessage(templateText));
};

ps.OnStreamDown += async (id, down) =>
{
    string templateText = channelsById[id].OfflineText;
    await http.PostAsync(channelsById[id].WebhookUrl, DiscordMessage(templateText));
};

ps.OnTitleChange += async (id, change) =>
{
    string templateText = channelsById[id].TitleChangeText;
    string text = templateText
        .Replace("%OLD_TITLE%", change.OldTitle)
        .Replace("%NEW_TITLE%", change.NewTitle);

    await http.PostAsync(channelsById[id].WebhookUrl, DiscordMessage(text));
};

ps.OnGameChange += async (id, change) =>
{
    string templateText = channelsById[id].GameChangeText;
    string text = templateText
        .Replace("%OLD_GAME%", change.OldGame)
        .Replace("%NEW_GAME%", change.NewGame)
        .Replace("%OLD_GAME_ID%", change.OldGameId.ToString())
        .Replace("%NEW_GAME_ID%", change.NewGameId.ToString());

    await http.PostAsync(channelsById[id].WebhookUrl, DiscordMessage(text));
};

Console.ReadLine();

StringContent DiscordMessage(string text)
{
    string json = $$"""
                    {"content": "{{text}}"}
                    """;

    return new(json, Encoding.UTF8, "application/json");
}

record ChannelTarget(
    long ChannelId, 
    string WebhookUrl, 
    string LiveText, 
    string GameChangeText, 
    string TitleChangeText, 
    string OfflineText
);
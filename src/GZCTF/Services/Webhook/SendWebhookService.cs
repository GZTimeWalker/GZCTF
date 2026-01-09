using System.Text.Json;
using GZCTF.Models.Data;
using Microsoft.Extensions.Logging;

namespace GZCTF.Services.Webhook;

public class Models
{
    public class DiscordWebhookMessage
    {
        public string? Content { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
        public List<DiscordEmbed>? Embeds { get; set; }
    }

    public class DiscordEmbed
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? Color { get; set; }
        public List<DiscordEmbedField>? Fields { get; set; }
        public DiscordEmbedFooter? Footer { get; set; }
        public string? Timestamp { get; set; }
    }

    public class DiscordEmbedField
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool? Inline { get; set; }
    }

    public class DiscordEmbedFooter
    {
        public string Text { get; set; } = string.Empty;
    }
}

public class SendWebhookService(IHttpClientFactory httpClientFactory, ILogger<SendWebhookService> logger) : ISendWebhookService
{
    public async Task SendGameEventAsync(GameEvent gameEvent, string webhookUrl)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            var message = CreateMessage(gameEvent);
            if (message == null) return;

            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(webhookUrl, content);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send webhook: {StatusCode}, {Reason}", response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending webhook");
        }
    }

    private Models.DiscordWebhookMessage? CreateMessage(GameEvent gameEvent)
    {
        // Only handle specific events to avoid noise
        if (gameEvent.Type != EventType.FlagSubmit && 
            gameEvent.Type != EventType.CheatDetected &&
            gameEvent.Type != EventType.FirstBlood &&
            gameEvent.Type != EventType.SecondBlood &&
            gameEvent.Type != EventType.ThirdBlood)
        {
            return null; 
        }

        var embed = new Models.DiscordEmbed
        {
            Timestamp = gameEvent.PublishTimeUtc.ToString("o"),
        };

        switch (gameEvent.Type)
        {
            case EventType.FlagSubmit:
                // Assuming Values: [Result, Answer, ChallengeName, ChallengeId]
                // We typically only want to notify on Accepted for solves, but GameEvent logs all submissions?
                // Wait, typically Scoreboard events are separate or filtered.
                // If it's just a log, we might want to filter for success.
                // Let's check Values[0] which is AnswerResult.
                if (gameEvent.Values.Count > 0 && gameEvent.Values[0] == AnswerResult.Accepted.ToString())
                {
                    embed.Title = "Challenge Solved! 🚩";
                    embed.Color = 0x00FF00; // Green
                    embed.Description = $"**{gameEvent.Team?.Name ?? "Unknown Team"}** solved **{gameEvent.Values.ElementAtOrDefault(2) ?? "Unknown Challenge"}**";
                    if (gameEvent.User != null)
                    {
                        embed.Fields = new List<Models.DiscordEmbedField>
                        {
                            new() { Name = "User", Value = gameEvent.User.UserName ?? "Unknown", Inline = true }
                        };
                    }
                }
                else
                {
                    return null; // Don't notify on wrong answers
                }
                break;
            case EventType.CheatDetected:
                embed.Title = "Cheat Detected! 🚨";
                embed.Color = 0xFF0000; // Red
                embed.Description = $"Cheat detected for team **{gameEvent.Team?.Name}**.\nDetails: {string.Join(", ", gameEvent.Values)}";
                break;
            // FirstBlood, SecondBlood, ThirdBlood are usually handled as submissions with special type? 
            // Or maybe separate EventType if I look at the Enum.
            // Let's check EventType enum.
            // GameEvent.cs: public class GameEvent : FormattableData<EventType>
            // Checking EventType enum in Api.ts or elsewhere.
            default:
                // Check if it is a Blood event
                 if (gameEvent.Type.ToString().Contains("Blood"))
                 {
                     embed.Title = $"{gameEvent.Type} 🩸";
                     embed.Color = 0xFFD700; // Gold
                     embed.Description = $"**{gameEvent.Team?.Name}** achieved {gameEvent.Type} on **{gameEvent.Values.ElementAtOrDefault(2)}**!";
                 }
                 else 
                 {
                     return null;
                 }
                 break;
        }

        return new Models.DiscordWebhookMessage
        {
            Embeds = new List<Models.DiscordEmbed> { embed }
        };
    }
}

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

    public async Task SendNoticeAsync(GameNotice notice, string webhookUrl)
    {
        try
        {
            using var client = httpClientFactory.CreateClient();
            var message = CreateNoticeMessage(notice);
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
                logger.LogError("Failed to send webhook notice: {StatusCode}, {Reason}", response.StatusCode, response.ReasonPhrase);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending webhook notice");
        }
    }

    private Models.DiscordWebhookMessage? CreateMessage(GameEvent gameEvent)
    {
        // Only handle specific events to avoid noise
        if (gameEvent.Type != EventType.FlagSubmit && 
            gameEvent.Type != EventType.CheatDetected)
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
                // User requested to ONLY notify for First/Second/Third Blood.
                // Bloods are handled via GameNotice (CreateNoticeMessage), so we disable
                // the generic "Challenge Solved!" notification here entirely.
                return null;
            case EventType.CheatDetected:
                embed.Title = "Cheat Detected! 🚨";
                embed.Color = 0xFF0000; // Red
                embed.Description = $"Cheat detected for team **{gameEvent.Team?.Name}**.\nDetails: {string.Join(", ", gameEvent.Values ?? [])}";
                embed.Footer = new Models.DiscordEmbedFooter { Text = gameEvent.Game?.Title ?? "Unknown Game" };
                break;
            default:
                 return null;
        }

        return new Models.DiscordWebhookMessage
        {
            Embeds = new List<Models.DiscordEmbed> { embed }
        };
    }

    private Models.DiscordWebhookMessage? CreateNoticeMessage(GameNotice notice)
    {
        var embed = new Models.DiscordEmbed
        {
            Timestamp = notice.PublishTimeUtc.ToString("o")
        };

        // Custom formatting for Blood notices
        if (notice.Type is NoticeType.FirstBlood or NoticeType.SecondBlood or NoticeType.ThirdBlood)
        {
            switch (notice.Type)
            {
                case NoticeType.FirstBlood:
                    embed.Title = "First Blood! 🥇";
                    embed.Color = 0xFFD700; // Gold
                    break;
                case NoticeType.SecondBlood:
                    embed.Title = "Second Blood! 🥈";
                    embed.Color = 0xC0C0C0; // Silver
                    break;
                case NoticeType.ThirdBlood:
                    embed.Title = "Third Blood! 🥉";
                    embed.Color = 0xCD7F32; // Bronze
                    break;
            }
            
            // Values: [TeamName, ChallengeName]
            var teamName = notice.Values?.ElementAtOrDefault(0) ?? "Unknown Team";
            var challengeName = notice.Values?.ElementAtOrDefault(1) ?? "Unknown Challenge";
            
            string prefix = notice.Type switch
            {
                NoticeType.FirstBlood => "First Blood! ",
                NoticeType.SecondBlood => "Second Blood! ",
                NoticeType.ThirdBlood => "Third Blood! ",
                _ => ""
            };

            embed.Description = $"{prefix}**{teamName}** solved **{challengeName}**";
            embed.Footer = new Models.DiscordEmbedFooter { Text = notice.Game?.Title ?? "Unknown Game" };
        }
        else
        {
             // Standard notices
             embed.Title = $"{notice.Type} 🎯";
             embed.Color = notice.Type switch
             {
                 NoticeType.NewHint => 0x3498DB,      // Blue
                 NoticeType.NewChallenge => 0x2ECC71, // Green
                 _ => 0x95A5A6                        // Gray for others
             };
             
             embed.Description = notice.Type switch
             {
                 NoticeType.NewChallenge => $"New challenge released: **{notice.Values?.FirstOrDefault()}**",
                 NoticeType.NewHint => $"New hint released for challenge **{notice.Values?.FirstOrDefault()}**",
                 NoticeType.Normal => notice.Values?.FirstOrDefault() ?? "New announcement",
                 _ => notice.Values?.Count > 0 ? string.Join(", ", notice.Values) : notice.Type.ToString()
             };
             embed.Footer = new Models.DiscordEmbedFooter { Text = notice.Game?.Title ?? "Unknown Game" };
        }

        return new Models.DiscordWebhookMessage
        {
            Embeds = new List<Models.DiscordEmbed> { embed }
        };
    }
}

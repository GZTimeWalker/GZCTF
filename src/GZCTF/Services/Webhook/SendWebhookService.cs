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

public class SendWebhookService(ILogger<SendWebhookService> logger) : ISendWebhookService
{
    private const int ContentLimit = 2000;
    private const int EmbedTitleLimit = 256;
    private const int EmbedDescriptionLimit = 4096;
    private const int EmbedFooterLimit = 2048;
    private const int EmbedFieldNameLimit = 256;
    private const int EmbedFieldValueLimit = 1024;
    private const int EmbedFieldCountLimit = 25;
    private const int EmbedTotalCharLimit = 6000;

    private static readonly HttpClient WebhookClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    public async Task SendGameEventAsync(GameEvent gameEvent, string webhookUrl)
    {
        try
        {
            var message = CreateMessage(gameEvent);
            if (message == null) return;

            await SendAsync(webhookUrl, message, "event");
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
            var message = CreateNoticeMessage(notice);
            if (message == null) return;

            await SendAsync(webhookUrl, message, "notice");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending webhook notice");
        }
    }

    private async Task SendAsync(string webhookUrl, Models.DiscordWebhookMessage message, string kind)
    {
        if (!Uri.TryCreate(webhookUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            logger.LogWarning("Skip invalid Discord webhook URL for {Kind}", kind);
            return;
        }

        SanitizeMessage(message);

        using var content = new StringContent(
            JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await WebhookClient.PostAsync(uri, content);

        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync();
        logger.LogError(
            "Failed to send webhook {Kind}: {StatusCode}, {Reason}, Body: {Body}",
            kind,
            (int)response.StatusCode,
            response.ReasonPhrase ?? "Unknown",
            Truncate(responseBody, 400));
    }

    private static string Truncate(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text ?? string.Empty;

        return text[..maxLength];
    }

    private static void SanitizeMessage(Models.DiscordWebhookMessage message)
    {
        message.Content = Truncate(message.Content, ContentLimit);

        if (message.Embeds is not { Count: > 0 })
            return;

        foreach (var embed in message.Embeds)
        {
            embed.Title = Truncate(embed.Title, EmbedTitleLimit);
            embed.Description = Truncate(embed.Description, EmbedDescriptionLimit);

            if (embed.Footer is not null)
                embed.Footer.Text = Truncate(embed.Footer.Text, EmbedFooterLimit);

            if (embed.Fields is { Count: > 0 })
            {
                if (embed.Fields.Count > EmbedFieldCountLimit)
                    embed.Fields = embed.Fields.Take(EmbedFieldCountLimit).ToList();

                foreach (var field in embed.Fields)
                {
                    field.Name = Truncate(field.Name, EmbedFieldNameLimit);
                    field.Value = Truncate(field.Value, EmbedFieldValueLimit);
                }
            }

            var totalChars = (embed.Title?.Length ?? 0) +
                             (embed.Description?.Length ?? 0) +
                             (embed.Footer?.Text.Length ?? 0) +
                             (embed.Fields?.Sum(f => f.Name.Length + f.Value.Length) ?? 0);

            if (totalChars > EmbedTotalCharLimit && !string.IsNullOrEmpty(embed.Description))
            {
                var overflow = totalChars - EmbedTotalCharLimit;
                embed.Description = Truncate(embed.Description, Math.Max(0, embed.Description.Length - overflow));
            }
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

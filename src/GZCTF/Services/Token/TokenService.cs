using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using GZCTF.Repositories.Interface;
using GZCTF.Services.Config;
using Microsoft.IdentityModel.Tokens;

namespace GZCTF.Services.Token;

/// <summary>
/// Service for handling token generation and validation.
/// </summary>
public class TokenService(IConfigService configService, IApiTokenRepository apiTokenRepository) : ITokenService
{
    static readonly JsonSerializerOptions Options = new() { WriteIndented = false };

    public async Task<string> GenerateToken(ApiToken apiToken, CancellationToken cancellationToken = default)
    {
        var ctx = await configService.GetApiTokenContext(cancellationToken);

        var payload = new Dictionary<string, object>
        {
            { JwtRegisteredClaimNames.Jti, apiToken.Id.ToString() },
            { JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds() }
        };

        if (apiToken.ExpiresAt.HasValue)
            payload.Add(JwtRegisteredClaimNames.Exp, apiToken.ExpiresAt.Value.ToUnixTimeSeconds());

        var json = JsonSerializer.Serialize(payload, Options);
        var base64Payload = Base64UrlEncoder.Encode(json);

        var signature = ctx.Sign(base64Payload);

        return $"{base64Payload}.{signature}";
    }

    public async Task<bool> ValidateToken(string token, CancellationToken cancellationToken = default)
    {
        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        var base64Payload = parts[0];
        var signature = parts[1];

        try
        {
            if (string.IsNullOrEmpty(base64Payload) || string.IsNullOrEmpty(signature))
                return false;

            var jsonPayload = Base64UrlEncoder.Decode(base64Payload);
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonPayload);

            if (payload is null
                || !payload.TryGetValue(JwtRegisteredClaimNames.Jti, out var jtiElement)
                || jtiElement.ValueKind != JsonValueKind.String)
                return false;

            if (!Guid.TryParse(jtiElement.GetString(), out var jti))
                return false;

            var apiToken = await apiTokenRepository.GetTokenByIdAsync(jti, cancellationToken);
            if (apiToken is null || apiToken.IsRevoked)
                return false;

            var now = DateTimeOffset.UtcNow;
            if (apiToken.ExpiresAt.HasValue && apiToken.ExpiresAt.Value < now)
                return false;

            if (payload.TryGetValue(JwtRegisteredClaimNames.Exp, out var expElement)
                && expElement.TryGetInt64(out var exp)
                && DateTimeOffset.FromUnixTimeSeconds(exp) < now)
                return false;

            var ctx = await configService.GetApiTokenContext(cancellationToken);
            if (!ctx.Verify(base64Payload, signature))
                return false;

            await apiTokenRepository.RecordTokenUsageAsync(apiToken.Id, cancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

using System.Net;
using System.Net.Sockets;
using System.Text;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Services;

/// <summary>
/// BackgroundService that runs one TCP listener per configured honeypot port.
/// On each connection: optionally send a banner, read up to a small probe buffer
/// with a short timeout, attribute via HoneypotService, and close.
///
/// Low-interaction by design — the goal is detection, not full protocol emulation.
/// </summary>
public class HoneypotPortListenerService(
    IServiceScopeFactory scopeFactory,
    IOptions<HoneypotConfig> config,
    ILogger<HoneypotPortListenerService> logger) : BackgroundService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);
    private const int MaxProbeBytes = 1024;
    private const int MaxProbeEncodedBytes = 64;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cfg = config.Value;

        if (!cfg.Enabled || cfg.Ports.Count == 0)
        {
            logger.LogInformation("Honeypot port listeners disabled.");
            return;
        }

        var address = IPAddress.TryParse(cfg.ListenAddress, out var parsed) ? parsed : IPAddress.Any;

        var tasks = cfg.Ports
            .Where(p => p.Enabled && p.Port is > 0 and < 65536 && !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => RunListener(p, address, stoppingToken))
            .ToList();

        if (tasks.Count == 0)
        {
            logger.LogInformation("Honeypot enabled but no valid port entries configured.");
            return;
        }

        await Task.WhenAll(tasks);
    }

    private async Task RunListener(HoneypotPort port, IPAddress address, CancellationToken stoppingToken)
    {
        TcpListener listener;
        try
        {
            listener = new TcpListener(address, port.Port);
            listener.Start();
            logger.LogInformation("Honeypot listening on {Addr}:{Port} ({Name})", address, port.Port, port.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to bind honeypot {Name} on {Addr}:{Port}", port.Name, address, port.Port);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Honeypot accept failed on {Name}", port.Name);
                    continue;
                }

                _ = HandleConnection(client, port, stoppingToken);
            }
        }
        finally
        {
            try { listener.Stop(); } catch { /* ignore */ }
        }
    }

    private async Task HandleConnection(TcpClient client, HoneypotPort port, CancellationToken stoppingToken)
    {
        var bait = $"{port.Name}:{port.Port}";
        IPAddress? remote = null;
        string? probe = null;

        try
        {
            remote = (client.Client.RemoteEndPoint as IPEndPoint)?.Address;

            using (client)
            {
                var stream = client.GetStream();

                if (!string.IsNullOrEmpty(port.Banner))
                {
                    var bannerBytes = Encoding.ASCII.GetBytes(port.Banner);
                    using var bannerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    bannerCts.CancelAfter(ProbeTimeout);
                    try { await stream.WriteAsync(bannerBytes, bannerCts.Token); }
                    catch { /* client may have hung up */ }
                }

                var buffer = new byte[MaxProbeBytes];
                using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                probeCts.CancelAfter(ProbeTimeout);
                try
                {
                    var read = await stream.ReadAsync(buffer, probeCts.Token);
                    if (read > 0)
                        probe = EncodeProbe(buffer.AsSpan(0, read));
                }
                catch { /* timeout or disconnect — fine */ }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Honeypot connection error on {Bait}", bait);
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var honeypot = scope.ServiceProvider.GetRequiredService<IHoneypotService>();
            await honeypot.RecordTcpHit(remote, bait, probe, token: stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Honeypot record failed for {Bait}", bait);
        }
    }

    private static string EncodeProbe(ReadOnlySpan<byte> bytes)
    {
        var len = Math.Min(bytes.Length, MaxProbeEncodedBytes);
        var slice = bytes[..len];
        return $"len={bytes.Length} hex={Convert.ToHexString(slice)} ascii={SafeAscii(slice)}";
    }

    private static string SafeAscii(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
            sb.Append(b is >= 0x20 and < 0x7f ? (char)b : '.');
        return sb.ToString();
    }
}

using System.Collections.Concurrent;
using System.Text;

namespace CTFServer.Services;

public class WebhookPushService : IHostedService
{
    private readonly ILogger<WebhookPushService> _logger;
    private readonly static ConcurrentQueue<(string Body, string TargetUri, HttpMethod Method)>
        _messageQueue = new();
    private readonly static SemaphoreSlim _semaphore = new(0, 4);
    private readonly static CancellationTokenSource _cancellationTokenSource = new();
    private static readonly HttpClient _httpClient = new();

    public WebhookPushService(ILogger<WebhookPushService> logger)
    {
        _logger = logger;
    }

    private static async Task PushWebhookAsync(string body, string targetUri, HttpMethod method, CancellationToken cancellationToken)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var message = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(targetUri)
        };

        if (message.Method != HttpMethod.Get && message.Method != HttpMethod.Delete)
        {
            message.Content = content;
        }

        await _httpClient.SendAsync(message, cancellationToken);
    }

    public static void QueueWebhook(string body, string targetUri, HttpMethod method)
    {
        _messageQueue.Enqueue((body, targetUri, method));
        if (_semaphore.CurrentCount < 4)
        {
            try
            {
                _semaphore.Release();
            }
            catch { /* ignored */ }
        }
    }

    private async void WorkerTask(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(cancellationToken);
                while (_messageQueue.TryDequeue(out var webhooks))
                {
                    try
                    {
                        await PushWebhookAsync(webhooks.Body, webhooks.TargetUri, webhooks.Method, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to push webhooks to {webhooks.TargetUri}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        for (var i = 0; i < 4; i++)
        {
            WorkerTask(_cancellationTokenSource.Token);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cancellationTokenSource.Cancel();
        return Task.CompletedTask;
    }
}

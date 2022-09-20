using Microsoft.Extensions.Caching.Distributed;

namespace CTFServer.Repositories;

public class WebhookRepository : RepositoryBase
{
    private readonly IDistributedCache cache;
    private readonly ILogger<WebhookRepository> logger;

    public WebhookRepository(IDistributedCache _cache,
        ILogger<WebhookRepository> _logger,
        AppDbContext dbContext) : base(dbContext)
    {
        cache = _cache;
        logger = _logger;
    }
}

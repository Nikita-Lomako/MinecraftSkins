using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MinecraftSkins.Domain.Models;
using MinecraftSkins.Infrastructure.Configuration;
using MinecraftSkins.Infrastructure.Data;

namespace MinecraftSkins.Infrastructure.Services;

public interface IQueryHistoryService
{
    Task LogSearchAsync(string? userId, string queryText, int resultCount, CancellationToken cancellationToken = default);
    Task CleanupAsync(CancellationToken cancellationToken = default);
}

public class QueryHistoryService : IQueryHistoryService
{
    private readonly AppDbContext _db;
    private readonly QueryHistoryOptions _options;
    private readonly ILogger<QueryHistoryService> _logger;

    public QueryHistoryService(AppDbContext db, IOptions<QueryHistoryOptions> options, ILogger<QueryHistoryService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task LogSearchAsync(string? userId, string queryText, int resultCount, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            return; // ✅ Не логируем пустые запросы
        }

        try
        {
            _db.SearchQueryHistories.Add(new SearchQueryHistory
            {
                UserId = userId,
                QueryText = queryText.Trim(),
                ResultCount = resultCount,
                Endpoint = "skins",
                CreatedAtUtc = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Игнорируем ошибки логирования — не должны ломать основной функционал
            _logger.LogWarning(ex, "Failed to log search query");
        }
    }

    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddDays(-Math.Max(1, _options.RetentionDays));
        var oldRows = await _db.SearchQueryHistories.Where(x => x.CreatedAtUtc < threshold).ToListAsync(cancellationToken);
        if (oldRows.Count > 0)
        {
            _db.SearchQueryHistories.RemoveRange(oldRows);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var total = await _db.SearchQueryHistories.CountAsync(cancellationToken);
        if (total <= _options.MaxRows)
        {
            return;
        }

        var rowsToDelete = total - _options.MaxRows;
        var overflow = await _db.SearchQueryHistories.OrderBy(x => x.CreatedAtUtc).Take(rowsToDelete).ToListAsync(cancellationToken);
        _db.SearchQueryHistories.RemoveRange(overflow);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

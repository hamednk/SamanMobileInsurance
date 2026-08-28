using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Infrastructure.Persistence;

namespace SamanMobileInsurance.Infrastructure.Logging;

public class AuditLogger : IAuditLogger
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpContextAccessor _http;
    private readonly ICurrentUser _current;

    public AuditLogger(IServiceScopeFactory scopeFactory, IHttpContextAccessor http, ICurrentUser current)
    {
        _scopeFactory = scopeFactory;
        _http = http;
        _current = current;
    }

    public async Task LogAsync(string action, string entityName, string? entityId, CancellationToken cancellationToken = default)
    {
        // Use an isolated DbContext so audit SaveChanges never re-saves
        // tracked entities (e.g. InsurancePolicy RowVersion) from the request scope.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ctx = _http.HttpContext;

        db.AuditLogs.Add(new Domain.Entities.AuditLog
        {
            UserId = _current.UserId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            IpAddress = ctx?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(ctx?.Request.Headers.UserAgent.ToString(), 512),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}

using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Festivals;

public record SalesFestivalDto(
    Guid Id,
    string Title,
    string Description,
    int RequiredIssuedCount,
    string RewardText,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record UpsertSalesFestivalRequest(
    string Title,
    string Description,
    int RequiredIssuedCount,
    string RewardText,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool IsActive);

public record StoreFestivalStatusDto(
    bool HasActiveFestival,
    string Message,
    Guid? FestivalId,
    string? Title,
    string? Description,
    string? RewardText,
    int RequiredIssuedCount,
    int CurrentIssuedCount,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool TargetReached);

public record FestivalStoreProgressDto(
    Guid StoreId,
    string StoreName,
    string ManagerName,
    string Mobile,
    int IssuedCount,
    int RequiredIssuedCount,
    bool TargetReached,
    string RewardText);

public class SalesFestivalService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly ICurrentUser _current;

    public SalesFestivalService(IApplicationDbContext db, IAuditLogger audit, ICurrentUser current)
    {
        _db = db;
        _audit = audit;
        _current = current;
    }

    public async Task<IReadOnlyList<SalesFestivalDto>> ListAsync(CancellationToken cancellationToken) =>
        await _db.SalesFestivals.AsNoTracking()
            .OrderByDescending(f => f.StartsAt)
            .Select(f => Map(f))
            .ToListAsync(cancellationToken);

    public async Task<SalesFestivalDto> CreateAsync(UpsertSalesFestivalRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var entity = new SalesFestival
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            RequiredIssuedCount = request.RequiredIssuedCount,
            RewardText = request.RewardText.Trim(),
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.SalesFestivals.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("festival-create", nameof(SalesFestival), entity.Id.ToString(), cancellationToken);
        return Map(entity);
    }

    public async Task<SalesFestivalDto> UpdateAsync(Guid id, UpsertSalesFestivalRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        var entity = await _db.SalesFestivals.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException("جشنواره یافت نشد.");
        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.RequiredIssuedCount = request.RequiredIssuedCount;
        entity.RewardText = request.RewardText.Trim();
        entity.StartsAt = request.StartsAt;
        entity.EndsAt = request.EndsAt;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("festival-update", nameof(SalesFestival), entity.Id.ToString(), cancellationToken);
        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.SalesFestivals.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new NotFoundException("جشنواره یافت نشد.");
        _db.SalesFestivals.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("festival-delete", nameof(SalesFestival), id.ToString(), cancellationToken);
    }

    public async Task<StoreFestivalStatusDto> GetStoreStatusAsync(CancellationToken cancellationToken)
    {
        if (_current.StoreId is null)
        {
            throw new ForbiddenAppException();
        }

        var now = DateTimeOffset.UtcNow;
        var festival = await _db.SalesFestivals.AsNoTracking()
            .Where(f => f.IsActive && f.StartsAt <= now && f.EndsAt >= now)
            .OrderByDescending(f => f.StartsAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (festival is null)
        {
            return new StoreFestivalStatusDto(
                false,
                "در حال حاضر جشنواره‌ای فعال نیست.",
                null, null, null, null, 0, 0, null, null, false);
        }

        var issuedCount = await _db.InsurancePolicies.AsNoTracking()
            .CountAsync(
                p => p.StoreId == _current.StoreId &&
                     p.Status == PolicyStatus.Issued &&
                     p.IssueDate != null &&
                     p.IssueDate >= festival.StartsAt &&
                     p.IssueDate <= festival.EndsAt,
                cancellationToken);

        var reached = issuedCount >= festival.RequiredIssuedCount;
        var message = reached
            ? $"تبریک! هدف جشنواره «{festival.Title}» محقق شد. پاداش: {festival.RewardText}"
            : $"جشنواره «{festival.Title}»: با صدور {festival.RequiredIssuedCount} بیمه‌نامه، {festival.RewardText} دریافت می‌کنید. پیشرفت شما: {issuedCount} از {festival.RequiredIssuedCount}";

        return new StoreFestivalStatusDto(
            true,
            message,
            festival.Id,
            festival.Title,
            festival.Description,
            festival.RewardText,
            festival.RequiredIssuedCount,
            issuedCount,
            festival.StartsAt,
            festival.EndsAt,
            reached);
    }

    public async Task<IReadOnlyList<FestivalStoreProgressDto>> GetStoreProgressAsync(
        Guid festivalId,
        bool onlyTargetReached,
        CancellationToken cancellationToken)
    {
        var festival = await _db.SalesFestivals.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == festivalId, cancellationToken)
            ?? throw new NotFoundException("جشنواره یافت نشد.");

        var stores = await _db.Stores.AsNoTracking()
            .Where(s => !s.IsDeleted && s.IsActive)
            .Select(s => new
            {
                s.Id,
                s.StoreName,
                ManagerName = s.ManagerFirstName + " " + s.ManagerLastName,
                s.Mobile1
            })
            .ToListAsync(cancellationToken);

        var issuedByStore = await _db.InsurancePolicies.AsNoTracking()
            .Where(p =>
                p.Status == PolicyStatus.Issued &&
                p.IssueDate != null &&
                p.IssueDate >= festival.StartsAt &&
                p.IssueDate <= festival.EndsAt)
            .GroupBy(p => p.StoreId)
            .Select(g => new { StoreId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoreId, x => x.Count, cancellationToken);

        var rows = stores
            .Select(s =>
            {
                issuedByStore.TryGetValue(s.Id, out var count);
                var reached = count >= festival.RequiredIssuedCount;
                return new FestivalStoreProgressDto(
                    s.Id,
                    s.StoreName,
                    s.ManagerName,
                    s.Mobile1,
                    count,
                    festival.RequiredIssuedCount,
                    reached,
                    festival.RewardText);
            })
            .Where(r => !onlyTargetReached || r.TargetReached)
            .OrderByDescending(r => r.TargetReached)
            .ThenByDescending(r => r.IssuedCount)
            .ThenBy(r => r.StoreName)
            .ToList();

        return rows;
    }

    private static void Validate(UpsertSalesFestivalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationAppException("عنوان جشنواره الزامی است.");
        }

        if (request.RequiredIssuedCount < 1)
        {
            throw new ValidationAppException("تعداد بیمه‌نامه هدف باید حداقل ۱ باشد.");
        }

        if (request.EndsAt <= request.StartsAt)
        {
            throw new ValidationAppException("تاریخ پایان باید بعد از تاریخ شروع باشد.");
        }

        if (string.IsNullOrWhiteSpace(request.RewardText))
        {
            throw new ValidationAppException("متن پاداش الزامی است.");
        }
    }

    private static SalesFestivalDto Map(SalesFestival f) => new(
        f.Id, f.Title, f.Description, f.RequiredIssuedCount, f.RewardText,
        f.StartsAt, f.EndsAt, f.IsActive, f.CreatedAt);
}

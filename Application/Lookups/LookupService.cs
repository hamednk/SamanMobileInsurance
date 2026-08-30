using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Lookups;

public record LookupItemDto(Guid Id, string Name);
public record CityLookupDto(Guid Id, string Name, Guid ProvinceId);

public class LookupService
{
    private readonly IApplicationDbContext _db;

    public LookupService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<LookupItemDto>> ProvincesAsync(CancellationToken cancellationToken) =>
        await _db.Provinces.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new LookupItemDto(p.Id, p.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CityLookupDto>> CitiesAsync(Guid? provinceId, CancellationToken cancellationToken)
    {
        var query = _db.Cities.AsNoTracking().AsQueryable();
        if (provinceId is not null)
        {
            query = query.Where(c => c.ProvinceId == provinceId);
        }

        return await query.OrderBy(c => c.Name)
            .Select(c => new CityLookupDto(c.Id, c.Name, c.ProvinceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> BrandsAsync(CancellationToken cancellationToken) =>
        await _db.MobileBrands.AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new LookupItemDto(b.Id, b.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupItemDto>> ModelsAsync(Guid brandId, CancellationToken cancellationToken) =>
        await _db.MobileModels.AsNoTracking()
            .Where(m => m.BrandId == brandId && m.IsActive && !m.IsDeleted)
            .OrderBy(m => m.Name)
            .Select(m => new LookupItemDto(m.Id, m.Name))
            .ToListAsync(cancellationToken);
}

public record StoreDashboardDto(
    int TodayPolicies,
    int MonthPolicies,
    decimal TotalSalesRial,
    int AwaitingPayment,
    int Issued);

public class StoreDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;

    public StoreDashboardService(IApplicationDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<StoreDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var storeId = _current.StoreId ?? throw new Common.ForbiddenAppException();
        var tehranNow = Common.IranDateTime.TehranNow;
        var startToday = new DateTimeOffset(tehranNow.Date, tehranNow.Offset).ToUniversalTime();
        var startMonth = new DateTimeOffset(new DateTime(tehranNow.Year, tehranNow.Month, 1), tehranNow.Offset).ToUniversalTime();

        var query = _db.InsurancePolicies.AsNoTracking().Where(p => p.StoreId == storeId);
        var today = await query.CountAsync(p => p.CreatedAt >= startToday, cancellationToken);
        var month = await query.CountAsync(p => p.CreatedAt >= startMonth, cancellationToken);
        var sales = await query
            .Where(p => p.Status == PolicyStatus.Issued)
            .SumAsync(p => (decimal?)p.CustomerChargedRial, cancellationToken) ?? 0;
        var awaiting = await query.CountAsync(p => p.Status == PolicyStatus.AwaitingPayment, cancellationToken);
        var issued = await query.CountAsync(p => p.Status == PolicyStatus.Issued, cancellationToken);

        return new StoreDashboardDto(today, month, sales, awaiting, issued);
    }
}

using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Stores;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Admin;

public record AdminDashboardDto(
    int TotalStores,
    int ActiveStores,
    int TodayPolicies,
    int MonthPolicies,
    decimal TotalPremiumRial,
    int NewPhones,
    int UsedPhones,
    int SuccessfulPayments,
    int FailedPayments,
    IReadOnlyList<DailySalesPoint> DailySales,
    IReadOnlyList<MonthlySalesPoint> MonthlySales,
    IReadOnlyList<ProvinceSalesPoint> ProvinceSales,
    IReadOnlyList<TopStorePoint> TopStores);

public record DailySalesPoint(string Date, int Count, decimal AmountRial);
public record MonthlySalesPoint(string Month, int Count, decimal AmountRial);
public record ProvinceSalesPoint(string Province, int Count, decimal AmountRial);
public record TopStorePoint(Guid StoreId, string StoreName, int Count, decimal AmountRial);

public record StoreFilter(
    int Page,
    int PageSize,
    string? Search,
    Guid? ProvinceId,
    Guid? CityId,
    DateOnly? From,
    DateOnly? To,
    string? SortBy,
    string? SortDirection);

public record AdminStoreListItem(
    Guid Id,
    string StoreName,
    string ManagerName,
    string NationalCode,
    string Mobile,
    string Province,
    string City,
    DateTimeOffset CreatedAt,
    bool IsActive);

public record CreateStoreByAdminRequest(
    string StoreName,
    string ManagerFirstName,
    string ManagerLastName,
    string NationalCode,
    DateOnly BirthDate,
    string Mobile1,
    string? Mobile2,
    Guid ProvinceId,
    Guid CityId,
    string Address,
    string PostalCode,
    string Username,
    string Password,
    bool IsActive);

public record UpdateStoreRequest(
    string StoreName,
    string ManagerFirstName,
    string ManagerLastName,
    string Mobile1,
    string? Mobile2,
    Guid ProvinceId,
    Guid CityId,
    string Address,
    string PostalCode,
    bool IsActive);

public record NamedItemDto(Guid Id, string Name, bool IsActive);
public record ModelItemDto(Guid Id, Guid BrandId, string BrandName, string Name, bool IsActive);
public record CreateNamedItemRequest(string Name, bool IsActive = true);
public record CreateModelRequest(Guid BrandId, string Name, bool IsActive = true);

public record AdminUserListItem(Guid Id, string Username, string Role, bool IsActive, DateTimeOffset CreatedAt);
public record CreateUserRequest(string Username, string Password, UserRole Role, bool IsActive);
public record AdminSetPasswordRequest(string NewPassword, string ConfirmPassword);

public record SettingDto(string Key, string Value, string? Description);
public record UpdateSettingRequest(string Value);

public record RateDto(Guid Id, InsuranceType InsuranceType, decimal MinPriceRial, decimal MaxPriceRial, decimal RatePercent, bool IsActive);
public record UpsertRateRequest(InsuranceType InsuranceType, decimal MinPriceRial, decimal MaxPriceRial, decimal RatePercent, bool IsActive);

public class AdminDashboardService
{
    private readonly IApplicationDbContext _db;

    public AdminDashboardService(IApplicationDbContext db) => _db = db;

    public async Task<AdminDashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var tehranNow = IranDateTime.TehranNow;
        var startToday = new DateTimeOffset(tehranNow.Date, tehranNow.Offset).ToUniversalTime();
        var startMonth = new DateTimeOffset(new DateTime(tehranNow.Year, tehranNow.Month, 1), tehranNow.Offset).ToUniversalTime();
        var start30 = startToday.AddDays(-29);

        var stores = _db.Stores.AsNoTracking().Where(s => !s.IsDeleted);
        var totalStores = await stores.CountAsync(cancellationToken);
        var activeStores = await stores.CountAsync(s => s.IsActive, cancellationToken);

        var policies = _db.InsurancePolicies.AsNoTracking();
        var today = await policies.CountAsync(p => p.CreatedAt >= startToday, cancellationToken);
        var month = await policies.CountAsync(p => p.CreatedAt >= startMonth, cancellationToken);
        var premium = await policies.Where(p => p.Status == PolicyStatus.Issued)
            .SumAsync(p => (decimal?)p.PremiumRial, cancellationToken) ?? 0;
        var newPhones = await policies.CountAsync(p => p.InsuranceType == InsuranceType.New && p.Status == PolicyStatus.Issued, cancellationToken);
        var usedPhones = await policies.CountAsync(p => p.InsuranceType == InsuranceType.Used && p.Status == PolicyStatus.Issued, cancellationToken);

        var payments = _db.Payments.AsNoTracking();
        var ok = await payments.CountAsync(p => p.Status == PaymentStatus.Paid, cancellationToken);
        var fail = await payments.CountAsync(p => p.Status == PaymentStatus.Failed, cancellationToken);

        var issued = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Store).ThenInclude(s => s.Province)
            .Where(p => p.Status == PolicyStatus.Issued && p.IssueDate != null && p.IssueDate >= start30)
            .ToListAsync(cancellationToken);

        var daily = issued
            .GroupBy(p => IranDateTime.ToJalaliDate(p.IssueDate!.Value))
            .Select(g => new DailySalesPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderBy(x => x.Date)
            .ToList();

        var monthlySource = await _db.InsurancePolicies.AsNoTracking()
            .Where(p => p.Status == PolicyStatus.Issued && p.IssueDate != null)
            .Select(p => new { p.IssueDate, p.PremiumRial })
            .ToListAsync(cancellationToken);

        var monthly = monthlySource
            .GroupBy(p => IranDateTime.ToJalaliDate(p.IssueDate!.Value)[..7])
            .Select(g => new MonthlySalesPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderBy(x => x.Month)
            .TakeLast(12)
            .ToList();

        var province = issued
            .GroupBy(p => p.Store.Province.Name)
            .Select(g => new ProvinceSalesPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderByDescending(x => x.AmountRial)
            .Take(10)
            .ToList();

        var topSource = await _db.InsurancePolicies.AsNoTracking()
            .Where(p => p.Status == PolicyStatus.Issued)
            .Select(p => new { p.StoreId, StoreName = p.Store.StoreName, p.PremiumRial })
            .ToListAsync(cancellationToken);

        var top = topSource
            .GroupBy(p => new { p.StoreId, p.StoreName })
            .Select(g => new TopStorePoint(g.Key.StoreId, g.Key.StoreName, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderByDescending(x => x.AmountRial)
            .Take(10)
            .ToList();

        return new AdminDashboardDto(
            totalStores, activeStores, today, month, premium, newPhones, usedPhones, ok, fail,
            daily, monthly, province, top);
    }
}

public class AdminStoreService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasherService _hasher;
    private readonly IAuditLogger _audit;

    public AdminStoreService(IApplicationDbContext db, IPasswordHasherService hasher, IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<PagedResult<AdminStoreListItem>> ListAsync(StoreFilter filter, CancellationToken cancellationToken)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var query = _db.Stores.AsNoTracking()
            .Include(s => s.Province)
            .Include(s => s.City)
            .Where(s => !s.IsDeleted);

        if (filter.ProvinceId is not null) query = query.Where(s => s.ProvinceId == filter.ProvinceId);
        if (filter.CityId is not null) query = query.Where(s => s.CityId == filter.CityId);
        if (filter.From is not null)
        {
            var from = filter.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= from);
        }
        if (filter.To is not null)
        {
            var to = filter.To.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt <= to);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var t = filter.Search.Trim();
            query = query.Where(s =>
                s.StoreName.Contains(t) ||
                s.ManagerFirstName.Contains(t) ||
                s.ManagerLastName.Contains(t) ||
                s.NationalCode.Contains(t) ||
                s.Mobile1.Contains(t) ||
                s.Username.Contains(t));
        }

        var desc = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (filter.SortBy?.ToLowerInvariant()) switch
        {
            "storename" => desc ? query.OrderByDescending(s => s.StoreName) : query.OrderBy(s => s.StoreName),
            "createdat" => desc ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
            _ => desc ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new AdminStoreListItem(
                s.Id,
                s.StoreName,
                s.ManagerFirstName + " " + s.ManagerLastName,
                s.NationalCode,
                s.Mobile1,
                s.Province.Name,
                s.City.Name,
                s.CreatedAt,
                s.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminStoreListItem>
        {
            Items = items,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, Total = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) }
        };
    }

    public async Task<StoreProfileDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.AsNoTracking().Include(s => s.Province).Include(s => s.City)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("فروشگاه یافت نشد.");
        return StoreService.Map(store);
    }

    public async Task<StoreProfileDto> CreateAsync(CreateStoreByAdminRequest request, CancellationToken cancellationToken)
    {
        var register = new RegisterStoreRequest(
            request.StoreName, request.ManagerFirstName, request.ManagerLastName, request.NationalCode,
            request.BirthDate, request.Mobile1, request.Mobile2, request.ProvinceId, request.CityId,
            request.Address, request.PostalCode, request.Username, request.Password,
            Guid.Empty, string.Empty);
        var service = new StoreService(_db, _hasher, _audit);
        var created = await service.RegisterAsync(register, cancellationToken);
        if (!request.IsActive)
        {
            return await SetActiveAsync(created.Id, false, cancellationToken);
        }
        return created;
    }

    public async Task<StoreProfileDto> UpdateAsync(Guid id, UpdateStoreRequest request, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("فروشگاه یافت نشد.");

        store.StoreName = request.StoreName.Trim();
        store.ManagerFirstName = request.ManagerFirstName.Trim();
        store.ManagerLastName = request.ManagerLastName.Trim();
        store.Mobile1 = request.Mobile1;
        store.Mobile2 = request.Mobile2;
        store.ProvinceId = request.ProvinceId;
        store.CityId = request.CityId;
        store.Address = request.Address.Trim();
        store.PostalCode = request.PostalCode;
        store.IsActive = request.IsActive;
        store.User.IsActive = request.IsActive;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("store-update", nameof(Store), id.ToString(), cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<StoreProfileDto> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var store = await _db.Stores.Include(s => s.User)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("فروشگاه یافت نشد.");
        store.IsActive = isActive;
        store.User.IsActive = isActive;
        store.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(isActive ? "store-activate" : "store-deactivate", nameof(Store), id.ToString(), cancellationToken);
        return await GetAsync(id, cancellationToken);
    }
}

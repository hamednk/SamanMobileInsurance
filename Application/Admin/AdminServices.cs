using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Admin;

public class AdminCatalogService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;

    public AdminCatalogService(IApplicationDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<NamedItemDto>> BrandsAsync(CancellationToken cancellationToken) =>
        await _db.MobileBrands.AsNoTracking().Where(b => !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new NamedItemDto(b.Id, b.Name, b.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<NamedItemDto> CreateBrandAsync(CreateNamedItemRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await _db.MobileBrands.AnyAsync(b => !b.IsDeleted && b.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            throw new ValidationAppException("این برند قبلاً ثبت شده است.");
        }

        var entity = new MobileBrand { Name = name, IsActive = request.IsActive, CreatedAt = DateTimeOffset.UtcNow };
        _db.MobileBrands.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("brand-create", nameof(MobileBrand), entity.Id.ToString(), cancellationToken);
        return new NamedItemDto(entity.Id, entity.Name, entity.IsActive);
    }

    public async Task<NamedItemDto> UpdateBrandAsync(Guid id, CreateNamedItemRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.MobileBrands.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("برند یافت نشد.");
        var name = request.Name.Trim();
        if (await _db.MobileBrands.AnyAsync(b => !b.IsDeleted && b.Id != id && b.Name.ToLower() == name.ToLower(), cancellationToken))
        {
            throw new ValidationAppException("این برند قبلاً ثبت شده است.");
        }

        entity.Name = name;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new NamedItemDto(entity.Id, entity.Name, entity.IsActive);
    }

    public async Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.MobileBrands.FirstOrDefaultAsync(b => b.Id == id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("برند یافت نشد.");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("brand-delete", nameof(MobileBrand), id.ToString(), cancellationToken);
    }

    public async Task<IReadOnlyList<ModelItemDto>> ModelsAsync(Guid? brandId, CancellationToken cancellationToken)
    {
        var query = _db.MobileModels.AsNoTracking().Where(m => !m.IsDeleted);
        if (brandId is not null) query = query.Where(m => m.BrandId == brandId);
        return await query.OrderBy(m => m.Name)
            .Select(m => new ModelItemDto(m.Id, m.BrandId, m.Brand.Name, m.Name, m.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<ModelItemDto> CreateModelAsync(CreateModelRequest request, CancellationToken cancellationToken)
    {
        if (!await _db.MobileBrands.AnyAsync(b => b.Id == request.BrandId && !b.IsDeleted, cancellationToken))
        {
            throw new ValidationAppException("برند معتبر نیست.");
        }

        var name = request.Name.Trim();
        if (await _db.MobileModels.AnyAsync(
                m => !m.IsDeleted && m.BrandId == request.BrandId && m.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            throw new ValidationAppException("این مدل برای برند انتخاب‌شده قبلاً ثبت شده است.");
        }

        var entity = new MobileModel
        {
            BrandId = request.BrandId,
            Name = name,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.MobileModels.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("model-create", nameof(MobileModel), entity.Id.ToString(), cancellationToken);
        var brandName = await _db.MobileBrands.AsNoTracking()
            .Where(b => b.Id == entity.BrandId)
            .Select(b => b.Name)
            .FirstAsync(cancellationToken);
        return new ModelItemDto(entity.Id, entity.BrandId, brandName, entity.Name, entity.IsActive);
    }

    public async Task<ModelItemDto> UpdateModelAsync(Guid id, CreateNamedItemRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.MobileModels.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("مدل یافت نشد.");
        var name = request.Name.Trim();
        if (await _db.MobileModels.AnyAsync(
                m => !m.IsDeleted && m.BrandId == entity.BrandId && m.Id != id && m.Name.ToLower() == name.ToLower(),
                cancellationToken))
        {
            throw new ValidationAppException("این مدل برای این برند قبلاً ثبت شده است.");
        }

        entity.Name = name;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        var brandName = await _db.MobileBrands.AsNoTracking()
            .Where(b => b.Id == entity.BrandId)
            .Select(b => b.Name)
            .FirstAsync(cancellationToken);
        return new ModelItemDto(entity.Id, entity.BrandId, brandName, entity.Name, entity.IsActive);
    }

    public async Task DeleteModelAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _db.MobileModels.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("مدل یافت نشد.");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("model-delete", nameof(MobileModel), id.ToString(), cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemNamed>> ProvincesAsync(CancellationToken cancellationToken) =>
        await _db.Provinces.AsNoTracking().OrderBy(p => p.Name)
            .Select(p => new LookupItemNamed(p.Id, p.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupItemNamed>> CitiesAsync(Guid? provinceId, CancellationToken cancellationToken)
    {
        var query = _db.Cities.AsNoTracking().AsQueryable();
        if (provinceId is not null) query = query.Where(c => c.ProvinceId == provinceId);
        return await query.OrderBy(c => c.Name).Select(c => new LookupItemNamed(c.Id, c.Name)).ToListAsync(cancellationToken);
    }
}

public record LookupItemNamed(Guid Id, string Name);

public class AdminUserService
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasherService _hasher;
    private readonly IAuditLogger _audit;

    public AdminUserService(IApplicationDbContext db, IPasswordHasherService hasher, IAuditLogger audit)
    {
        _db = db;
        _hasher = hasher;
        _audit = audit;
    }

    public async Task<PagedResult<AdminUserListItem>> ListAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Users.AsNoTracking().Where(u => !u.IsDeleted);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            query = query.Where(u => u.Username.Contains(t));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new AdminUserListItem(u.Id, u.Username, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserListItem>
        {
            Items = items,
            Pagination = new PaginationMeta { Page = page, PageSize = pageSize, Total = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) }
        };
    }

    public async Task<AdminUserListItem> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (request.Role == UserRole.Store)
        {
            throw new ValidationAppException("کاربر فروشگاه باید از طریق ثبت فروشگاه ایجاد شود.");
        }

        var username = request.Username.Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username && !u.IsDeleted, cancellationToken))
        {
            throw new ConflictException("نام کاربری تکراری است.");
        }

        var user = new User
        {
            Username = username,
            PasswordHash = _hasher.Hash(request.Password),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("user-create", nameof(User), user.Id.ToString(), cancellationToken);
        return new AdminUserListItem(user.Id, user.Username, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("کاربر یافت نشد.");
        user.IsActive = isActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPasswordAsync(Guid id, AdminSetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new ValidationAppException("رمز عبور باید حداقل ۸ کاراکتر باشد.");
        }

        if (request.NewPassword != request.ConfirmPassword)
        {
            throw new ValidationAppException("تکرار رمز عبور مطابقت ندارد.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken)
            ?? throw new NotFoundException("کاربر یافت نشد.");

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("user-password-change", nameof(User), user.Id.ToString(), cancellationToken);
    }
}

public class AdminSettingsService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;

    public AdminSettingsService(IApplicationDbContext db, IAuditLogger audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SettingDto>> ListAsync(CancellationToken cancellationToken) =>
        await _db.AppSettings.AsNoTracking()
            .OrderBy(s => s.Key)
            .Select(s => new SettingDto(s.Key, s.Value, s.Description))
            .ToListAsync(cancellationToken);

    public async Task<SettingDto> UpdateAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken)
            ?? throw new NotFoundException("تنظیم یافت نشد.");
        setting.Value = value;
        setting.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("setting-update", nameof(AppSetting), key, cancellationToken);
        return new SettingDto(setting.Key, setting.Value, setting.Description);
    }

    public async Task<IReadOnlyList<RateDto>> RatesAsync(CancellationToken cancellationToken) =>
        await _db.InsuranceRateConfigurations.AsNoTracking()
            .OrderBy(r => r.InsuranceType).ThenBy(r => r.MinPriceRial)
            .Select(r => new RateDto(r.Id, r.InsuranceType, r.MinPriceRial, r.MaxPriceRial, r.RatePercent, r.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<RateDto> CreateRateAsync(UpsertRateRequest request, CancellationToken cancellationToken)
    {
        var entity = new InsuranceRateConfiguration
        {
            InsuranceType = request.InsuranceType,
            MinPriceRial = request.MinPriceRial,
            MaxPriceRial = request.MaxPriceRial,
            RatePercent = request.RatePercent,
            IsActive = request.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.InsuranceRateConfigurations.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return new RateDto(entity.Id, entity.InsuranceType, entity.MinPriceRial, entity.MaxPriceRial, entity.RatePercent, entity.IsActive);
    }

    public async Task<RateDto> UpdateRateAsync(Guid id, UpsertRateRequest request, CancellationToken cancellationToken)
    {
        var entity = await _db.InsuranceRateConfigurations.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("نرخ یافت نشد.");
        entity.InsuranceType = request.InsuranceType;
        entity.MinPriceRial = request.MinPriceRial;
        entity.MaxPriceRial = request.MaxPriceRial;
        entity.RatePercent = request.RatePercent;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return new RateDto(entity.Id, entity.InsuranceType, entity.MinPriceRial, entity.MaxPriceRial, entity.RatePercent, entity.IsActive);
    }
}

public class AdminQueryService
{
    private readonly IApplicationDbContext _db;

    public AdminQueryService(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<CustomerListItem>> CustomersAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            query = query.Where(c => c.FirstName.Contains(t) || c.LastName.Contains(t) || c.NationalCode.Contains(t) || c.Mobile.Contains(t));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CustomerListItem(c.Id, c.FirstName, c.LastName, c.NationalCode, c.Mobile, c.CreatedAt))
            .ToListAsync(cancellationToken);
        return Page(items, page, pageSize, total);
    }

    public async Task<PagedResult<PaymentListItem>> PaymentsAsync(int page, int pageSize, PaymentStatus? status, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Payments.AsNoTracking().Include(p => p.Policy).AsQueryable();
        if (status is not null) query = query.Where(p => p.Status == status);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PaymentListItem(p.Id, p.PolicyId, p.Policy.PolicyNumber, p.AmountRial, p.Status, p.TrackingCode, p.PaidAt, p.CreatedAt))
            .ToListAsync(cancellationToken);
        return Page(items, page, pageSize, total);
    }

    public async Task<PagedResult<AuditLogItem>> AuditLogsAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim();
            query = query.Where(a => a.Action.Contains(t) || a.EntityName.Contains(t));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AuditLogItem(a.Id, a.UserId, a.Action, a.EntityName, a.EntityId, a.IpAddress, a.CreatedAt))
            .ToListAsync(cancellationToken);
        return Page(items, page, pageSize, total);
    }

    private static PagedResult<T> Page<T>(IReadOnlyList<T> items, int page, int pageSize, int total) => new()
    {
        Items = items,
        Pagination = new PaginationMeta { Page = page, PageSize = pageSize, Total = total, TotalPages = (int)Math.Ceiling(total / (double)pageSize) }
    };
}

public record CustomerListItem(Guid Id, string FirstName, string LastName, string NationalCode, string Mobile, DateTimeOffset CreatedAt);
public record PaymentListItem(Guid Id, Guid PolicyId, string? PolicyNumber, decimal AmountRial, PaymentStatus Status, string? TrackingCode, DateTimeOffset? PaidAt, DateTimeOffset CreatedAt);
public record AuditLogItem(Guid Id, Guid? UserId, string Action, string EntityName, string? EntityId, string? IpAddress, DateTimeOffset CreatedAt);

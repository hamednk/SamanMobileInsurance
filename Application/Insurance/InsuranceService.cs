using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public class InsuranceService
{
    private static readonly PolicyStatus[] ActiveImeiStatuses =
    [
        PolicyStatus.Draft,
        PolicyStatus.AwaitingImages,
        PolicyStatus.AwaitingPayment,
        PolicyStatus.Paid,
        PolicyStatus.Issued
    ];

    private readonly IApplicationDbContext _db;
    private readonly PremiumCalculationService _premium;
    private readonly ICurrentUser _current;
    private readonly IAuditLogger _audit;

    public InsuranceService(
        IApplicationDbContext db,
        PremiumCalculationService premium,
        ICurrentUser current,
        IAuditLogger audit)
    {
        _db = db;
        _premium = premium;
        _current = current;
        _audit = audit;
    }

    public async Task<PolicyDto> CreateDraftAsync(CreatePolicyRequest request, CancellationToken cancellationToken)
    {
        var store = await RequireActiveStoreAsync(cancellationToken);
        await EnsureBrandModelAsync(request.BrandId, request.ModelId, cancellationToken);
        await EnsureImeiAvailableAsync(request.Imei1, request.Imei2, null, cancellationToken);

        var quote = await _premium.QuoteAsync(request.InsuranceType, request.MobilePriceRial, cancellationToken);
        var startDate = ResolveStartDate(request.InsuranceType, request.StartDate);
        var customer = await UpsertCustomerAsync(request.Customer, cancellationToken);

        var policy = new InsurancePolicy
        {
            StoreId = store.Id,
            CustomerId = customer.Id,
            InsuranceType = request.InsuranceType,
            BrandId = request.BrandId,
            ModelId = request.ModelId,
            MobilePriceRial = quote.MobilePriceRial,
            PremiumRial = quote.PremiumRial,
            CustomerChargedRial = StoreMarkup.ResolveCustomerCharged(quote.PremiumRial, request.CustomerChargedRial),
            Imei1 = request.Imei1,
            Imei2 = string.IsNullOrWhiteSpace(request.Imei2) ? null : request.Imei2,
            StartDate = startDate,
            Status = PolicyStatus.AwaitingImages,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.InsurancePolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("policy-create", nameof(InsurancePolicy), policy.Id.ToString(), cancellationToken);
        return await GetAsync(policy.Id, cancellationToken);
    }

    public async Task<PolicyDto> UpdateDraftAsync(Guid id, UpdatePolicyDraftRequest request, CancellationToken cancellationToken)
    {
        var policy = await LoadOwnedAsync(id, cancellationToken);

        if (policy.Status is not (PolicyStatus.Draft or PolicyStatus.AwaitingImages or PolicyStatus.AwaitingPayment))
        {
            throw new ValidationAppException("پس از پرداخت امکان ویرایش اطلاعات بیمه‌نامه وجود ندارد.");
        }

        await EnsureBrandModelAsync(request.BrandId, request.ModelId, cancellationToken);
        await EnsureImeiAvailableAsync(request.Imei1, request.Imei2, policy.Id, cancellationToken);

        var quote = await _premium.QuoteAsync(policy.InsuranceType, request.MobilePriceRial, cancellationToken);
        var startDate = policy.InsuranceType == InsuranceType.New
            ? ResolveStartDate(InsuranceType.New, request.StartDate)
            : policy.StartDate;

        var customer = await UpsertCustomerAsync(request.Customer, cancellationToken);

        policy.CustomerId = customer.Id;
        policy.BrandId = request.BrandId;
        policy.ModelId = request.ModelId;
        policy.MobilePriceRial = quote.MobilePriceRial;
        policy.PremiumRial = quote.PremiumRial;
        policy.CustomerChargedRial = StoreMarkup.ResolveCustomerCharged(quote.PremiumRial, request.CustomerChargedRial);
        policy.Imei1 = request.Imei1;
        policy.Imei2 = string.IsNullOrWhiteSpace(request.Imei2) ? null : request.Imei2;
        policy.StartDate = startDate;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("policy-update", nameof(InsurancePolicy), policy.Id.ToString(), cancellationToken);
        return await GetAsync(policy.Id, cancellationToken);
    }

    public async Task<PolicyDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var query = QueryPolicies();
        if (_current.IsStore)
        {
            query = query.Where(p => p.StoreId == _current.StoreId);
        }

        var policy = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

        return Map(policy);
    }

    public async Task<PagedResult<PolicyListItemDto>> ListMineAsync(
        int page,
        int pageSize,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        InsuranceType? insuranceType,
        PolicyStatus? status,
        PaymentStatus? paymentStatus,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Where(p => p.StoreId == _current.StoreId);

        query = ApplyStoreListFilters(query, search, fromDate, toDate, insuranceType, status, paymentStatus);

        var total = await query.CountAsync(cancellationToken);
        var today = IranDateTime.TehranToday;
        var rows = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.PolicyNumber,
                p.InsuranceType,
                p.Status,
                p.PaymentStatus,
                p.PremiumRial,
                p.CustomerChargedRial,
                CustomerName = p.Customer.FirstName + " " + p.Customer.LastName,
                BrandName = p.Brand.Name,
                ModelName = p.Model.Name,
                p.CreatedAt,
                p.IssueDate,
                p.EndDate,
                p.RenewedFromPolicyId,
                p.StartDate
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(p => new PolicyListItemDto(
            p.Id,
            p.PolicyNumber,
            p.InsuranceType,
            p.Status,
            p.PaymentStatus,
            p.PremiumRial,
            p.CustomerChargedRial,
            StoreMarkup.Profit(p.CustomerChargedRial, p.PremiumRial),
            p.CustomerName,
            p.BrandName,
            p.ModelName,
            p.CreatedAt,
            p.IssueDate,
            p.EndDate,
            p.RenewedFromPolicyId,
            CanRenewPolicy(p.Status, p.EndDate, p.StartDate, today))).ToList();

        return new PagedResult<PolicyListItemDto>
        {
            Items = items,
            Pagination = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    public async Task<IReadOnlyList<RenewalListItemDto>> ListRenewalsAsync(string track, CancellationToken cancellationToken)
    {
        if (_current.StoreId is null)
        {
            throw new ForbiddenAppException();
        }

        await ExpireDuePoliciesAsync(_current.StoreId.Value, cancellationToken);

        var today = IranDateTime.TehranToday;
        var normalized = string.Equals(track, "renewed", StringComparison.OrdinalIgnoreCase) ? "Renewed" : "Expired";

        var query = _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Customer)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Where(p => p.StoreId == _current.StoreId);

        if (normalized == "Renewed")
        {
            query = query.Where(p => p.RenewedFromPolicyId != null);
        }
        else
        {
            var todayStart = IranDateTime.TehranNow.Date;
            // منقضی‌شده: پایان پوشش رسیده (یا وضعیت Expired) و خودِ رکورد تمدید نیست
            query = query.Where(p =>
                p.RenewedFromPolicyId == null &&
                (p.Status == PolicyStatus.Expired ||
                 (p.Status == PolicyStatus.Issued && p.EndDate != null && p.EndDate <= todayStart)));
        }

        var rows = await query
            .OrderByDescending(p => p.EndDate ?? p.CreatedAt)
            .Take(200)
            .Select(p => new
            {
                p.Id,
                p.PolicyNumber,
                p.InsuranceType,
                p.Status,
                p.PaymentStatus,
                p.PremiumRial,
                p.CustomerChargedRial,
                CustomerName = p.Customer.FirstName + " " + p.Customer.LastName,
                BrandName = p.Brand.Name,
                ModelName = p.Model.Name,
                p.CreatedAt,
                p.IssueDate,
                p.EndDate,
                p.RenewedFromPolicyId,
                p.StartDate
            })
            .ToListAsync(cancellationToken);

        return rows.Select(p => new RenewalListItemDto(
            p.Id,
            p.PolicyNumber,
            p.InsuranceType,
            p.Status,
            p.PaymentStatus,
            p.PremiumRial,
            p.CustomerChargedRial,
            StoreMarkup.Profit(p.CustomerChargedRial, p.PremiumRial),
            p.CustomerName,
            p.BrandName,
            p.ModelName,
            p.CreatedAt,
            p.IssueDate,
            p.EndDate,
            p.RenewedFromPolicyId,
            CanRenewPolicy(p.Status, p.EndDate, p.StartDate, today),
            normalized)).ToList();
    }

    public async Task ExpireDuePoliciesAsync(Guid storeId, CancellationToken cancellationToken)
    {
        var todayStart = IranDateTime.TehranNow.Date;
        var due = await _db.InsurancePolicies
            .Where(p =>
                p.StoreId == storeId &&
                p.Status == PolicyStatus.Issued &&
                p.EndDate != null &&
                p.EndDate <= todayStart)
            .ToListAsync(cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var policy in due)
        {
            policy.Status = PolicyStatus.Expired;
            policy.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PolicyDto> RenewAsync(Guid id, CancellationToken cancellationToken)
    {
        var store = await RequireActiveStoreAsync(cancellationToken);
        await ExpireDuePoliciesAsync(store.Id, cancellationToken);

        var source = await _db.InsurancePolicies
            .Include(p => p.Customer)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.StoreId == store.Id, cancellationToken)
            ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

        if (source.Status is not (PolicyStatus.Issued or PolicyStatus.Expired))
        {
            throw new ValidationAppException("فقط بیمه‌نامه‌های صادرشده یا منقضی‌شده قابل تمدید هستند.");
        }

        var today = IranDateTime.TehranToday;
        if (!IsCoverageEnded(source.EndDate, source.StartDate, today))
        {
            throw new ValidationAppException("تمدید فقط پس از رسیدن یا عبور از تاریخ پایان پوشش بیمه‌نامه امکان‌پذیر است.");
        }

        var openRenewal = await _db.InsurancePolicies.AnyAsync(
            p => p.RenewedFromPolicyId == source.Id &&
                 (p.Status == PolicyStatus.Draft ||
                  p.Status == PolicyStatus.AwaitingImages ||
                  p.Status == PolicyStatus.AwaitingPayment ||
                  p.Status == PolicyStatus.Paid),
            cancellationToken);
        if (openRenewal)
        {
            throw new ValidationAppException("برای این بیمه‌نامه یک تمدید ناتمام وجود دارد.");
        }

        await EnsureImeiAvailableAsync(source.Imei1, source.Imei2, source.Id, cancellationToken);

        // تمدید همیشه با نرخ گوشی کارکرده محاسبه می‌شود
        var quote = await _premium.QuoteAsync(InsuranceType.Used, source.MobilePriceRial, cancellationToken);
        var startDate = IranDateTime.TehranNow.Date;

        var renewal = new InsurancePolicy
        {
            StoreId = store.Id,
            CustomerId = source.CustomerId,
            InsuranceType = InsuranceType.Used,
            BrandId = source.BrandId,
            ModelId = source.ModelId,
            MobilePriceRial = quote.MobilePriceRial,
            PremiumRial = quote.PremiumRial,
            CustomerChargedRial = quote.PremiumRial,
            Imei1 = source.Imei1,
            Imei2 = source.Imei2,
            StartDate = startDate,
            RenewedFromPolicyId = source.Id,
            Status = PolicyStatus.AwaitingPayment,
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        foreach (var image in source.Images)
        {
            renewal.Images.Add(new InsuranceImage
            {
                ImageType = image.ImageType,
                FilePath = image.FilePath,
                FileName = image.FileName,
                ContentType = image.ContentType,
                UploadedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        if (source.Status == PolicyStatus.Issued)
        {
            source.Status = PolicyStatus.Expired;
            source.UpdatedAt = DateTimeOffset.UtcNow;
        }

        _db.InsurancePolicies.Add(renewal);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("policy-renew", nameof(InsurancePolicy), renewal.Id.ToString(), cancellationToken);
        return await GetAsync(renewal.Id, cancellationToken);
    }

    public async Task<PolicyDto> CancelAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await LoadOwnedAsync(id, cancellationToken);
        PolicyStateMachine.Ensure(policy.Status, PolicyStatus.Cancelled);
        policy.Status = PolicyStatus.Cancelled;
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("policy-cancel", nameof(InsurancePolicy), policy.Id.ToString(), cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<PolicyDto> SetCustomerChargedAsync(Guid id, decimal customerChargedRial, CancellationToken cancellationToken)
    {
        var policy = await LoadOwnedAsync(id, cancellationToken);
        if (policy.Status is PolicyStatus.Issued or PolicyStatus.Cancelled or PolicyStatus.Expired)
        {
            throw new ValidationAppException("پس از صدور، لغو یا انقضای بیمه‌نامه امکان تغییر مبلغ دریافتی وجود ندارد.");
        }

        policy.CustomerChargedRial = StoreMarkup.ResolveCustomerCharged(policy.PremiumRial, customerChargedRial);
        policy.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("policy-customer-charged", nameof(InsurancePolicy), policy.Id.ToString(), cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task EnsureImeiAvailableAsync(string imei1, string? imei2, Guid? excludePolicyId, CancellationToken cancellationToken)
    {
        if (!await IsImeiAvailableAsync(imei1, imei2, excludePolicyId, cancellationToken))
        {
            throw new ConflictException("این IMEI دارای بیمه‌نامه فعال است و امکان ثبت بیمه جدید وجود ندارد.");
        }
    }

    public async Task<bool> IsImeiAvailableAsync(string imei1, string? imei2, Guid? excludePolicyId, CancellationToken cancellationToken)
    {
        var query = _db.InsurancePolicies.AsNoTracking()
            .Where(p => ActiveImeiStatuses.Contains(p.Status));

        if (excludePolicyId is not null)
        {
            query = query.Where(p => p.Id != excludePolicyId);
        }

        return !await query.AnyAsync(p =>
            p.Imei1 == imei1 ||
            p.Imei2 == imei1 ||
            (imei2 != null && (p.Imei1 == imei2 || p.Imei2 == imei2)), cancellationToken);
    }

    public async Task<Customer?> FindCustomerAsync(string nationalCode, string mobile, CancellationToken cancellationToken)
    {
        return await _db.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.NationalCode == nationalCode || c.Mobile == mobile, cancellationToken);
    }

    private async Task<Customer> UpsertCustomerAsync(CustomerInput input, CancellationToken cancellationToken)
    {
        var existing = await _db.Customers
            .FirstOrDefaultAsync(c => c.NationalCode == input.NationalCode || c.Mobile == input.Mobile, cancellationToken);

        if (existing is not null)
        {
            existing.FirstName = input.FirstName.Trim();
            existing.LastName = input.LastName.Trim();
            existing.NationalCode = input.NationalCode;
            existing.BirthDate = input.BirthDate;
            existing.Mobile = input.Mobile;
            existing.Address = input.Address.Trim();
            existing.PostalCode = input.PostalCode;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            return existing;
        }

        var customer = new Customer
        {
            FirstName = input.FirstName.Trim(),
            LastName = input.LastName.Trim(),
            NationalCode = input.NationalCode,
            BirthDate = input.BirthDate,
            Mobile = input.Mobile,
            Address = input.Address.Trim(),
            PostalCode = input.PostalCode,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Customers.Add(customer);
        return customer;
    }

    private async Task<Store> RequireActiveStoreAsync(CancellationToken cancellationToken)
    {
        if (_current.StoreId is null)
        {
            throw new ForbiddenAppException();
        }

        var store = await _db.Stores.FirstOrDefaultAsync(
            s => s.Id == _current.StoreId && !s.IsDeleted, cancellationToken)
            ?? throw new ForbiddenAppException();

        if (!store.IsActive)
        {
            throw new ForbiddenAppException("فروشگاه شما غیرفعال است و امکان ثبت بیمه جدید وجود ندارد.");
        }

        return store;
    }

    private static IQueryable<InsurancePolicy> ApplyStoreListFilters(
        IQueryable<InsurancePolicy> query,
        string? search,
        DateOnly? fromDate,
        DateOnly? toDate,
        InsuranceType? insuranceType,
        PolicyStatus? status,
        PaymentStatus? paymentStatus)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                (p.PolicyNumber != null && p.PolicyNumber.Contains(term)) ||
                p.Customer.FirstName.Contains(term) ||
                p.Customer.LastName.Contains(term) ||
                p.Customer.NationalCode.Contains(term) ||
                p.Imei1.Contains(term));
        }

        if (fromDate is not null)
        {
            var start = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), IranDateTime.TehranNow.Offset).ToUniversalTime();
            query = query.Where(p => p.CreatedAt >= start);
        }

        if (toDate is not null)
        {
            var endExclusive = new DateTimeOffset(toDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), IranDateTime.TehranNow.Offset).ToUniversalTime();
            query = query.Where(p => p.CreatedAt < endExclusive);
        }

        if (insuranceType is not null) query = query.Where(p => p.InsuranceType == insuranceType);
        if (status is not null) query = query.Where(p => p.Status == status);
        if (paymentStatus is not null) query = query.Where(p => p.PaymentStatus == paymentStatus);

        return query;
    }

    private async Task EnsureBrandModelAsync(Guid brandId, Guid modelId, CancellationToken cancellationToken)
    {
        var model = await _db.MobileModels.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId && m.BrandId == brandId && m.IsActive && !m.IsDeleted, cancellationToken)
            ?? throw new ValidationAppException("برند یا مدل انتخاب‌شده معتبر نیست.");

        var brandOk = await _db.MobileBrands.AsNoTracking()
            .AnyAsync(b => b.Id == brandId && b.IsActive && !b.IsDeleted, cancellationToken);
        if (!brandOk)
        {
            throw new ValidationAppException("برند انتخاب‌شده معتبر نیست.");
        }

        _ = model;
    }

    private IQueryable<InsurancePolicy> QueryPolicies() =>
        _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.Customer)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Include(p => p.Images);

    private async Task<InsurancePolicy> LoadOwnedAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _db.InsurancePolicies.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

        if (_current.IsStore && policy.StoreId != _current.StoreId)
        {
            throw new ForbiddenAppException();
        }

        return policy;
    }

    public static DateTimeOffset ResolveStartDate(InsuranceType type, DateTimeOffset? requested)
    {
        if (type == InsuranceType.Used)
        {
            return IranDateTime.TehranNow.Date;
        }

        return requested ?? throw new ValidationAppException("برای گوشی آکبند، تاریخ شروع بیمه‌نامه الزامی است.");
    }

    public static bool CanRenewPolicy(
        PolicyStatus status,
        DateTimeOffset? endDate,
        DateTimeOffset startDate,
        DateOnly today)
    {
        if (status is not (PolicyStatus.Issued or PolicyStatus.Expired))
        {
            return false;
        }

        return IsCoverageEnded(endDate, startDate, today);
    }

    public static bool IsCoverageEnded(DateTimeOffset? endDate, DateTimeOffset startDate, DateOnly today)
    {
        var effectiveEnd = endDate ?? startDate.AddYears(1);
        var endLocal = TimeZoneInfo.ConvertTime(effectiveEnd, IranDateTime.TehranTimeZone);
        var endDay = DateOnly.FromDateTime(endLocal.DateTime);
        return endDay <= today;
    }

    public static PolicyDto Map(InsurancePolicy policy) => new(
        policy.Id,
        policy.PolicyNumber,
        policy.InsuranceType,
        policy.Status,
        policy.PaymentStatus,
        policy.MobilePriceRial,
        policy.PremiumRial,
        policy.CustomerChargedRial,
        StoreMarkup.Profit(policy.CustomerChargedRial, policy.PremiumRial),
        policy.Imei1,
        policy.Imei2,
        policy.StartDate,
        policy.EndDate,
        policy.IssueDate,
        policy.CreatedAt,
        policy.StoreId,
        policy.Store.StoreName,
        policy.CustomerId,
        policy.Customer.FirstName,
        policy.Customer.LastName,
        policy.Customer.NationalCode,
        policy.Customer.Mobile,
        policy.Customer.Address,
        policy.Customer.PostalCode,
        policy.Customer.BirthDate,
        policy.BrandId,
        policy.Brand.Name,
        policy.ModelId,
        policy.Model.Name,
        policy.PaymentTrackingCode,
        policy.RenewedFromPolicyId,
        CanRenewPolicy(policy.Status, policy.EndDate, policy.StartDate, IranDateTime.TehranToday),
        policy.Images.Select(i => new PolicyImageDto(i.Id, i.ImageType, i.FileName, i.UploadedAt)).ToList());
}

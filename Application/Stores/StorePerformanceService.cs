using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Stores;

public record StoreDailyPoint(string Date, int Count, decimal PremiumRial);
public record StoreBrandPoint(string Brand, int Count, decimal PremiumRial);

public record StorePerformanceReportDto(
    DateOnly From,
    DateOnly To,
    int IssuedCount,
    int RenewedCount,
    int NewPhoneCount,
    int UsedPhoneCount,
    int AwaitingPaymentCount,
    int CancelledCount,
    int TotalPoliciesInRange,
    /// <summary>مبلغ دریافتی از مشتری (جمع حق بیمه صادرشده)</summary>
    decimal CustomerReceivedRial,
    /// <summary>مبلغ قابل واریز به شرکت</summary>
    decimal CompanyRemittanceRial,
    decimal TotalMobilePriceRial,
    decimal AveragePremiumRial,
    /// <summary>درصد سهم فروشگاه از حق بیمه</summary>
    decimal StoreCommissionPercent,
    /// <summary>درصد سهم شرکت از حق بیمه</summary>
    decimal CompanyRemittancePercent,
    /// <summary>سود فروشگاه = دریافتی از مشتری − واریز به شرکت</summary>
    decimal StoreProfitRial,
    IReadOnlyList<StoreDailyPoint> Daily,
    IReadOnlyList<StoreBrandPoint> TopBrands);

public class StorePerformanceService
{
    public const string CommissionSettingKey = "StoreCommissionPercent";

    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _current;

    public StorePerformanceService(IApplicationDbContext db, ICurrentUser current)
    {
        _db = db;
        _current = current;
    }

    public async Task<StorePerformanceReportDto> GetAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var storeId = _current.StoreId ?? throw new ForbiddenAppException();
        if (to < from)
        {
            throw new ValidationAppException("تاریخ پایان نمی‌تواند قبل از تاریخ شروع باشد.");
        }

        if (to.DayNumber - from.DayNumber > 366)
        {
            throw new ValidationAppException("بازه گزارش حداکثر یک سال است.");
        }

        var offset = IranDateTime.TehranNow.Offset;
        var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), offset).ToUniversalTime();
        var endExclusive = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), offset).ToUniversalTime();

        var policies = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.StoreId == storeId && p.CreatedAt >= start && p.CreatedAt < endExclusive)
            .ToListAsync(cancellationToken);

        var issuedInPeriod = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.StoreId == storeId &&
                        p.Status == PolicyStatus.Issued &&
                        p.IssueDate != null &&
                        p.IssueDate >= start &&
                        p.IssueDate < endExclusive)
            .ToListAsync(cancellationToken);

        var storeSharePercent = await GetCommissionPercentAsync(cancellationToken);
        var companySharePercent = Math.Clamp(100m - storeSharePercent, 0m, 100m);
        var customerReceived = issuedInPeriod.Sum(p => p.PremiumRial);
        var mobilePrice = issuedInPeriod.Sum(p => p.MobilePriceRial);
        // سود فروشگاه = مبلغ دریافتی از مشتری − مبلغ واریزی به شرکت
        var companyRemittance = Math.Round(customerReceived * companySharePercent / 100m, 0, MidpointRounding.AwayFromZero);
        var storeProfit = customerReceived - companyRemittance;

        var daily = issuedInPeriod
            .GroupBy(p => IranDateTime.ToJalaliDate(p.IssueDate!.Value))
            .Select(g => new StoreDailyPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderBy(x => x.Date)
            .ToList();

        var brands = issuedInPeriod
            .GroupBy(p => p.Brand.Name)
            .Select(g => new StoreBrandPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        return new StorePerformanceReportDto(
            from,
            to,
            issuedInPeriod.Count,
            issuedInPeriod.Count(p => p.RenewedFromPolicyId != null),
            issuedInPeriod.Count(p => p.InsuranceType == InsuranceType.New),
            issuedInPeriod.Count(p => p.InsuranceType == InsuranceType.Used),
            policies.Count(p => p.Status == PolicyStatus.AwaitingPayment),
            policies.Count(p => p.Status == PolicyStatus.Cancelled),
            policies.Count,
            customerReceived,
            companyRemittance,
            mobilePrice,
            issuedInPeriod.Count == 0 ? 0 : Math.Round(customerReceived / issuedInPeriod.Count, 0, MidpointRounding.AwayFromZero),
            storeSharePercent,
            companySharePercent,
            storeProfit,
            daily,
            brands);
    }

    private async Task<decimal> GetCommissionPercentAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == CommissionSettingKey, cancellationToken);
        if (setting is null || !decimal.TryParse(setting.Value, out var percent) || percent < 0)
        {
            return 15m;
        }

        return percent;
    }
}

using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Insurance;
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
    /// <summary>مبلغ دریافتی از مشتری</summary>
    decimal CustomerReceivedRial,
    /// <summary>سهم شرکت (جمع حق بیمه محاسبه‌شده)</summary>
    decimal CompanyRemittanceRial,
    decimal TotalMobilePriceRial,
    /// <summary>سود فروشگاه = دریافتی از مشتری − سهم شرکت</summary>
    decimal StoreProfitRial,
    IReadOnlyList<StoreDailyPoint> Daily,
    IReadOnlyList<StoreBrandPoint> TopBrands);

public class StorePerformanceService
{
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

        var billedInPeriod = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.StoreId == storeId &&
                        (p.Status == PolicyStatus.Issued || p.Status == PolicyStatus.Paid) &&
                        (
                            (p.IssueDate != null && p.IssueDate >= start && p.IssueDate < endExclusive) ||
                            (p.Status == PolicyStatus.Paid && p.CreatedAt >= start && p.CreatedAt < endExclusive)
                        ))
            .ToListAsync(cancellationToken);

        var customerReceived = billedInPeriod.Sum(p => p.CustomerChargedRial);
        var companyRemittance = billedInPeriod.Sum(p => p.PremiumRial);
        var storeProfit = StoreMarkup.Profit(customerReceived, companyRemittance);
        var mobilePrice = billedInPeriod.Sum(p => p.MobilePriceRial);

        var daily = billedInPeriod
            .GroupBy(p => IranDateTime.ToJalaliDate(p.IssueDate ?? p.CreatedAt))
            .Select(g => new StoreDailyPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderBy(x => x.Date)
            .ToList();

        var brands = billedInPeriod
            .GroupBy(p => p.Brand.Name)
            .Select(g => new StoreBrandPoint(g.Key, g.Count(), g.Sum(x => x.PremiumRial)))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToList();

        return new StorePerformanceReportDto(
            from,
            to,
            billedInPeriod.Count,
            billedInPeriod.Count(p => p.RenewedFromPolicyId != null),
            billedInPeriod.Count(p => p.InsuranceType == InsuranceType.New),
            billedInPeriod.Count(p => p.InsuranceType == InsuranceType.Used),
            policies.Count(p => p.Status == PolicyStatus.AwaitingPayment),
            policies.Count(p => p.Status == PolicyStatus.Cancelled),
            policies.Count,
            customerReceived,
            companyRemittance,
            mobilePrice,
            storeProfit,
            daily,
            brands);
    }
}

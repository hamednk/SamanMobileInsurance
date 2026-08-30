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
    decimal AveragePremiumRial,
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

        var issuedInPeriod = await _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.StoreId == storeId &&
                        p.Status == PolicyStatus.Issued &&
                        p.IssueDate != null &&
                        p.IssueDate >= start &&
                        p.IssueDate < endExclusive)
            .ToListAsync(cancellationToken);

        var customerReceived = issuedInPeriod.Sum(p => p.CustomerChargedRial);
        var companyRemittance = issuedInPeriod.Sum(p => p.PremiumRial);
        var storeProfit = StoreMarkup.Profit(customerReceived, companyRemittance);
        var mobilePrice = issuedInPeriod.Sum(p => p.MobilePriceRial);

        var daily = issuedInPeriod
            .GroupBy(p => IranDateTime.ToJalaliDate(p.IssueDate!.Value))
            .Select(g => new StoreDailyPoint(g.Key, g.Count(), g.Sum(x => x.CustomerChargedRial)))
            .OrderBy(x => x.Date)
            .ToList();

        var brands = issuedInPeriod
            .GroupBy(p => p.Brand.Name)
            .Select(g => new StoreBrandPoint(g.Key, g.Count(), g.Sum(x => x.CustomerChargedRial)))
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
            issuedInPeriod.Count == 0 ? 0 : Math.Round(companyRemittance / issuedInPeriod.Count, 0, MidpointRounding.AwayFromZero),
            storeProfit,
            daily,
            brands);
    }
}

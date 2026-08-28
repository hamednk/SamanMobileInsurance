using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public class PremiumCalculationService
{
    public const string MaxPriceSettingKey = "MaxInsurablePriceRial";
    public const decimal DefaultMaxPriceRial = 10_000_000_000m;

    private readonly IApplicationDbContext _db;

    public PremiumCalculationService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<decimal> GetMaxInsurablePriceRialAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == MaxPriceSettingKey, cancellationToken);

        if (setting is null || !decimal.TryParse(setting.Value, out var value) || value <= 0)
        {
            return DefaultMaxPriceRial;
        }

        return value;
    }

    public async Task<PremiumQuote> QuoteAsync(InsuranceType type, decimal mobilePriceRial, CancellationToken cancellationToken = default)
    {
        var max = await GetMaxInsurablePriceRialAsync(cancellationToken);
        EnsurePriceAllowed(mobilePriceRial, max);

        var rates = await _db.InsuranceRateConfigurations.AsNoTracking()
            .Where(r => r.IsActive && r.InsuranceType == type)
            .ToListAsync(cancellationToken);

        return Quote(type, mobilePriceRial, rates, max);
    }

    public static void EnsurePriceAllowed(decimal mobilePriceRial, decimal maxPriceRial)
    {
        if (mobilePriceRial <= 0)
        {
            throw new ValidationAppException("قیمت موبایل باید بزرگ‌تر از صفر باشد.");
        }

        if (mobilePriceRial > maxPriceRial)
        {
            throw new BusinessRuleException("امکان ثبت بیمه برای موبایل‌های با ارزش بیش از ۱ میلیارد تومان وجود ندارد.");
        }
    }

    public static PremiumQuote Quote(
        InsuranceType type,
        decimal mobilePriceRial,
        IReadOnlyList<InsuranceRateConfiguration> rates,
        decimal maxPriceRial)
    {
        EnsurePriceAllowed(mobilePriceRial, maxPriceRial);

        var rate = rates
            .Where(r => r.IsActive && r.InsuranceType == type)
            .Where(r => mobilePriceRial >= r.MinPriceRial && mobilePriceRial <= r.MaxPriceRial)
            .OrderByDescending(r => r.MinPriceRial)
            .FirstOrDefault();

        if (rate is null)
        {
            throw new BusinessRuleException("نرخ بیمه‌ای برای این بازه قیمت تعریف نشده است.");
        }

        var premium = Math.Round(mobilePriceRial * rate.RatePercent / 100m, 0, MidpointRounding.AwayFromZero);
        return new PremiumQuote(mobilePriceRial, premium, rate.RatePercent, type);
    }

    public static IReadOnlyList<InsuranceRateConfiguration> DefaultRates() =>
    [
        New(1m, 999_999_999m, 1.4m),
        New(1_000_000_000m, 1_999_999_999m, 1.5m),
        New(2_000_000_000m, 4_999_999_999m, 2.3m),
        New(5_000_000_000m, 10_000_000_000m, 3.2m),
        Used(1m, 1_999_999_999m, 1.65m),
        Used(2_000_000_000m, 4_999_999_999m, 2.9m),
        Used(5_000_000_000m, 10_000_000_000m, 4.2m)
    ];

    private static InsuranceRateConfiguration New(decimal min, decimal max, decimal rate) => new()
    {
        InsuranceType = InsuranceType.New,
        MinPriceRial = min,
        MaxPriceRial = max,
        RatePercent = rate,
        IsActive = true
    };

    private static InsuranceRateConfiguration Used(decimal min, decimal max, decimal rate) => new()
    {
        InsuranceType = InsuranceType.Used,
        MinPriceRial = min,
        MaxPriceRial = max,
        RatePercent = rate,
        IsActive = true
    };
}

public record PremiumQuote(decimal MobilePriceRial, decimal PremiumRial, decimal RatePercent, InsuranceType InsuranceType);

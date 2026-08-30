using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;
using SamanMobileInsurance.Infrastructure.Auth;
using SamanMobileInsurance.Infrastructure.Persistence;

namespace SamanMobileInsurance.Infrastructure.Persistence;

public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, IConfiguration configuration, ILogger<DbSeeder> logger)
    {
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedGeoAsync(cancellationToken);
        await SeedRatesAsync(cancellationToken);
        await SeedSettingsAsync(cancellationToken);
        await SeedCatalogAsync(cancellationToken);
        await SeedAdminAsync(cancellationToken);
    }

    private async Task SeedGeoAsync(CancellationToken cancellationToken)
    {
        if (await _db.Provinces.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var (name, cities) in IranGeoData.Provinces)
        {
            var province = new Province { Name = name, CreatedAt = DateTimeOffset.UtcNow };
            foreach (var city in cities)
            {
                province.Cities.Add(new City { Name = city, CreatedAt = DateTimeOffset.UtcNow });
            }
            _db.Provinces.Add(province);
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Iran provinces and cities.");
    }

    private async Task SeedRatesAsync(CancellationToken cancellationToken)
    {
        if (await _db.InsuranceRateConfigurations.AnyAsync(cancellationToken))
        {
            return;
        }

        _db.InsuranceRateConfigurations.AddRange(PremiumCalculationService.DefaultRates());
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        await EnsureSettingAsync(
            PremiumCalculationService.MaxPriceSettingKey,
            PremiumCalculationService.DefaultMaxPriceRial.ToString(),
            "سقف قیمت قابل بیمه به ریال (پیش‌فرض معادل ۱ میلیارد تومان)",
            cancellationToken);

        var obsoleteCommission = await _db.AppSettings.FirstOrDefaultAsync(
            s => s.Key == "StoreCommissionPercent", cancellationToken);
        if (obsoleteCommission is not null)
        {
            _db.AppSettings.Remove(obsoleteCommission);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureSettingAsync(string key, string value, string description, CancellationToken cancellationToken)
    {
        var existing = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.Description, description, StringComparison.Ordinal))
            {
                existing.Description = description;
                existing.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        _db.AppSettings.Add(new AppSetting
        {
            Key = key,
            Value = value,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCatalogAsync(CancellationToken cancellationToken)
    {
        foreach (var (brandName, models) in MobileCatalogData.Items)
        {
            var brandId = await _db.MobileBrands
                .AsNoTracking()
                .Where(b => b.Name == brandName && !b.IsDeleted)
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (brandId is null)
            {
                var brand = new MobileBrand
                {
                    Name = brandName,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                _db.MobileBrands.Add(brand);
                await _db.SaveChangesAsync(cancellationToken);
                brandId = brand.Id;
                _db.ChangeTracker.Clear();
            }
            else
            {
                await _db.MobileBrands
                    .Where(b => b.Id == brandId.Value && !b.IsActive)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(b => b.IsActive, true)
                            .SetProperty(b => b.UpdatedAt, DateTimeOffset.UtcNow),
                        cancellationToken);
            }

            var existingNames = await _db.MobileModels
                .AsNoTracking()
                .Where(m => m.BrandId == brandId.Value && !m.IsDeleted)
                .Select(m => m.Name)
                .ToListAsync(cancellationToken);
            var existingSet = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);

            // Revive soft-deleted models that match the catalog instead of inserting duplicates.
            foreach (var modelName in models.Where(m => !existingSet.Contains(m)))
            {
                var revived = await _db.MobileModels
                    .Where(m =>
                        m.BrandId == brandId.Value &&
                        m.IsDeleted &&
                        m.Name.ToLower() == modelName.ToLower())
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(m => m.IsDeleted, false)
                            .SetProperty(m => m.DeletedAt, (DateTimeOffset?)null)
                            .SetProperty(m => m.IsActive, true)
                            .SetProperty(m => m.UpdatedAt, DateTimeOffset.UtcNow),
                        cancellationToken);

                if (revived > 0)
                {
                    existingSet.Add(modelName);
                }
            }

            var toAdd = models
                .Where(m => !existingSet.Contains(m))
                .Select(m => new MobileModel
                {
                    BrandId = brandId.Value,
                    Name = m,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                })
                .ToList();

            if (toAdd.Count == 0)
            {
                continue;
            }

            _db.MobileModels.AddRange(toAdd);
            await _db.SaveChangesAsync(cancellationToken);
            _db.ChangeTracker.Clear();
        }

        _logger.LogInformation("Mobile catalog seed upsert completed.");
    }

    private async Task SeedAdminAsync(CancellationToken cancellationToken)
    {
        if (await _db.Users.AnyAsync(u => u.Role == UserRole.Admin, cancellationToken))
        {
            return;
        }

        var username = _configuration["ADMIN_USERNAME"] ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
        var password = _configuration["ADMIN_PASSWORD"] ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("ADMIN_PASSWORD is not set. Admin user was not seeded.");
            return;
        }

        var hasher = new PasswordHasherService();
        _db.Users.Add(new User
        {
            Username = username,
            PasswordHash = hasher.Hash(password),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded admin user {Username}.", username);
    }
}

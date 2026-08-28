using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using SamanMobileInsurance.Domain.Entities;

namespace SamanMobileInsurance.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Store> Stores { get; }
    DbSet<Province> Provinces { get; }
    DbSet<City> Cities { get; }
    DbSet<Customer> Customers { get; }
    DbSet<MobileBrand> MobileBrands { get; }
    DbSet<MobileModel> MobileModels { get; }
    DbSet<InsurancePolicy> InsurancePolicies { get; }
    DbSet<InsuranceImage> InsuranceImages { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<InsuranceRateConfiguration> InsuranceRateConfigurations { get; }
    DbSet<AppSetting> AppSettings { get; }
    DbSet<SalesFestival> SalesFestivals { get; }
    ChangeTracker ChangeTracker { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}

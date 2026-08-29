using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SamanMobileInsurance.Domain.Entities;

namespace SamanMobileInsurance.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => x.Username).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Store).WithOne(s => s.User).HasForeignKey<Store>(s => s.UserId);
    }
}

public class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores");
        builder.Property(x => x.StoreName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ManagerFirstName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ManagerLastName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Mobile1).HasMaxLength(11).IsRequired();
        builder.Property(x => x.Mobile2).HasMaxLength(11);
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Username).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.NationalCode).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.HasIndex(x => x.ProvinceId);
        builder.HasIndex(x => x.CityId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IsActive).HasFilter("[IsDeleted] = 0");
        builder.HasOne(x => x.Province).WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.City).WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => x.ProvinceId);
        builder.HasIndex(x => new { x.ProvinceId, x.Name }).IsUnique();
        builder.HasOne(x => x.Province).WithMany(p => p.Cities).HasForeignKey(x => x.ProvinceId);
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.NationalCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Mobile).HasMaxLength(11).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => x.NationalCode);
        builder.HasIndex(x => x.Mobile);
        builder.HasIndex(x => x.CreatedAt);
    }
}

public class MobileBrandConfiguration : IEntityTypeConfiguration<MobileBrand>
{
    public void Configure(EntityTypeBuilder<MobileBrand> builder)
    {
        builder.ToTable("MobileBrands");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasIndex(x => new { x.IsActive, x.Name })
            .HasFilter("[IsDeleted] = 0");
    }
}

public class MobileModelConfiguration : IEntityTypeConfiguration<MobileModel>
{
    public void Configure(EntityTypeBuilder<MobileModel> builder)
    {
        builder.ToTable("MobileModels");
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.HasOne(x => x.Brand).WithMany(b => b.Models).HasForeignKey(x => x.BrandId);
        builder.HasIndex(x => new { x.BrandId, x.IsActive, x.Name })
            .HasFilter("[IsDeleted] = 0");
    }
}

public class InsurancePolicyConfiguration : IEntityTypeConfiguration<InsurancePolicy>
{
    public void Configure(EntityTypeBuilder<InsurancePolicy> builder)
    {
        builder.ToTable("InsurancePolicies");
        builder.Property(x => x.PolicyNumber).HasMaxLength(32);
        builder.Property(x => x.Imei1).HasMaxLength(15).IsRequired();
        builder.Property(x => x.Imei2).HasMaxLength(15);
        builder.Property(x => x.MobilePriceRial).HasPrecision(18, 0);
        builder.Property(x => x.PremiumRial).HasPrecision(18, 0);
        builder.Property(x => x.PaymentTrackingCode).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.PolicyNumber).IsUnique().HasFilter("[PolicyNumber] IS NOT NULL");
        builder.HasIndex(x => new { x.Imei1, x.Status });
        builder.HasIndex(x => new { x.Imei2, x.Status })
            .HasFilter("[Imei2] IS NOT NULL");
        builder.HasIndex(x => x.StoreId);
        builder.HasIndex(x => new { x.StoreId, x.CreatedAt })
            .IsDescending(false, true);
        builder.HasIndex(x => new { x.StoreId, x.Status, x.IssueDate });
        builder.HasIndex(x => new { x.StoreId, x.Status, x.EndDate });
        builder.HasIndex(x => new { x.Status, x.IssueDate });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.IssueDate);
        builder.HasIndex(x => x.PaymentStatus);
        builder.HasOne(x => x.Store).WithMany(s => s.Policies).HasForeignKey(x => x.StoreId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Customer).WithMany(c => c.Policies).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Model).WithMany().HasForeignKey(x => x.ModelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RenewedFromPolicy).WithMany().HasForeignKey(x => x.RenewedFromPolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.RenewedFromPolicyId);
        builder.HasIndex(x => x.EndDate);
    }
}

public class SalesFestivalConfiguration : IEntityTypeConfiguration<SalesFestival>
{
    public void Configure(EntityTypeBuilder<SalesFestival> builder)
    {
        builder.ToTable("SalesFestivals");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RewardText).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.IsActive, x.StartsAt, x.EndsAt });
        builder.HasIndex(x => x.StartsAt);
        builder.HasIndex(x => x.EndsAt);
    }
}

public class InsuranceImageConfiguration : IEntityTypeConfiguration<InsuranceImage>
{
    public void Configure(EntityTypeBuilder<InsuranceImage> builder)
    {
        builder.ToTable("InsuranceImages");
        builder.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.PolicyId, x.ImageType }).IsUnique();
        builder.HasOne(x => x.Policy).WithMany(p => p.Images).HasForeignKey(x => x.PolicyId);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(x => x.AmountRial).HasPrecision(18, 0);
        builder.Property(x => x.TransactionId).HasMaxLength(64);
        builder.Property(x => x.TrackingCode).HasMaxLength(64);
        builder.Property(x => x.Authority).HasMaxLength(128);
        builder.HasIndex(x => x.Authority);
        builder.HasIndex(x => new { x.PolicyId, x.Status });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.Policy).WithMany(p => p.Payments).HasForeignKey(x => x.PolicyId);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(64);
        builder.Property(x => x.IpAddress).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasOne(x => x.User).WithMany(u => u.AuditLogs).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasOne(x => x.User).WithMany(u => u.RefreshTokens).HasForeignKey(x => x.UserId);
        builder.Ignore(x => x.IsActive);
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        builder.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasOne(x => x.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(x => x.UserId);
    }
}

public class InsuranceRateConfigurationConfiguration : IEntityTypeConfiguration<InsuranceRateConfiguration>
{
    public void Configure(EntityTypeBuilder<InsuranceRateConfiguration> builder)
    {
        builder.ToTable("InsuranceRateConfigurations");
        builder.Property(x => x.MinPriceRial).HasPrecision(18, 0);
        builder.Property(x => x.MaxPriceRial).HasPrecision(18, 0);
        builder.Property(x => x.RatePercent).HasPrecision(5, 2);
    }
}

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("AppSettings");
        builder.Property(x => x.Key).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(300);
        builder.HasIndex(x => x.Key).IsUnique();
    }
}

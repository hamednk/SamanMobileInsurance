using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SamanMobileInsurance.Application.Admin;
using SamanMobileInsurance.Application.Auth;
using SamanMobileInsurance.Application.Festivals;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Application.Lookups;
using SamanMobileInsurance.Application.Payments;
using SamanMobileInsurance.Application.Reports;
using SamanMobileInsurance.Application.Stores;

namespace SamanMobileInsurance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<AuthService>();
        services.AddScoped<StoreService>();
        services.AddScoped<PremiumCalculationService>();
        services.AddScoped<InsuranceService>();
        services.AddScoped<InsuranceImageService>();
        services.AddScoped<PolicyIssuanceService>();
        services.AddScoped<PaymentService>();
        services.AddScoped<LookupService>();
        services.AddScoped<StoreDashboardService>();
        services.AddScoped<StorePerformanceService>();
        services.AddScoped<AdminDashboardService>();
        services.AddScoped<AdminStoreService>();
        services.AddScoped<AdminCatalogService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<AdminSettingsService>();
        services.AddScoped<AdminQueryService>();
        services.AddScoped<ReportService>();
        services.AddScoped<SalesFestivalService>();
        return services;
    }
}

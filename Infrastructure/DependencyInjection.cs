using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Payments;
using SamanMobileInsurance.Infrastructure.Auth;
using SamanMobileInsurance.Infrastructure.Logging;
using SamanMobileInsurance.Infrastructure.Persistence;
using SamanMobileInsurance.Infrastructure.Reports;
using SamanMobileInsurance.Infrastructure.Storage;

namespace SamanMobileInsurance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(5);
                sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            }));

        services.AddMemoryCache();
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<DbSeeder>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<INotificationService, MockNotificationService>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IExcelReportService, ExcelReportService>();
        services.AddScoped<IImageProcessor, ImageProcessor>();
        services.AddSingleton<ICaptchaService, CaptchaService>();

        var storage = new StorageOptions();
        configuration.GetSection("Storage").Bind(storage);
        if (string.IsNullOrWhiteSpace(storage.RootPath))
        {
            storage.RootPath = Path.Combine(AppContext.BaseDirectory, "data", "uploads");
        }
        services.AddSingleton(storage);
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        var payment = new PaymentOptions();
        configuration.GetSection("Payment").Bind(payment);
        services.AddSingleton(payment);

        return services;
    }
}

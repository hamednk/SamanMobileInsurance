using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Abstractions;

public interface IExcelReportService
{
    Task<byte[]> ExportInsuranceAsync(InsuranceReportFilter filter, CancellationToken cancellationToken = default);
}

public record InsuranceReportFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? ProvinceId,
    Guid? CityId,
    Guid? StoreId,
    InsuranceType? InsuranceType,
    PolicyStatus? Status,
    PaymentStatus? PaymentStatus,
    string? Search,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null);

using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Reports;

public record InsuranceReportRow(
    Guid Id,
    string? PolicyNumber,
    string StoreName,
    string ManagerName,
    string StoreMobile,
    string Province,
    string City,
    string StoreAddress,
    string CustomerFirstName,
    string CustomerLastName,
    string NationalCode,
    DateOnly BirthDate,
    string CustomerMobile,
    string CustomerAddress,
    string PostalCode,
    InsuranceType InsuranceType,
    string Brand,
    string Model,
    decimal MobilePriceRial,
    string Imei1,
    string? Imei2,
    DateTimeOffset? IssueDate,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    decimal PremiumRial,
    decimal CustomerChargedRial,
    decimal StoreProfitRial,
    PolicyStatus Status,
    PaymentStatus PaymentStatus,
    string? TransactionId,
    string? TrackingCode);

public record StoreReportRow(
    string StoreName,
    string ManagerName,
    string NationalCode,
    string Mobile,
    string Province,
    string City,
    DateTimeOffset CreatedAt,
    bool IsActive);

public class ReportService
{
    private readonly IApplicationDbContext _db;

    public ReportService(IApplicationDbContext db) => _db = db;

    public IQueryable<Domain.Entities.InsurancePolicy> InsuranceQuery(InsuranceReportFilter filter)
    {
        var query = _db.InsurancePolicies.AsNoTracking()
            .Include(p => p.Store).ThenInclude(s => s.Province)
            .Include(p => p.Store).ThenInclude(s => s.City)
            .Include(p => p.Customer)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .Include(p => p.Payments)
            .AsQueryable();

        if (filter.FromDate is not null)
        {
            var from = filter.FromDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt >= from);
        }
        if (filter.ToDate is not null)
        {
            var to = filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            query = query.Where(p => p.CreatedAt <= to);
        }
        if (filter.ProvinceId is not null) query = query.Where(p => p.Store.ProvinceId == filter.ProvinceId);
        if (filter.CityId is not null) query = query.Where(p => p.Store.CityId == filter.CityId);
        if (filter.StoreId is not null) query = query.Where(p => p.StoreId == filter.StoreId);
        if (filter.InsuranceType is not null) query = query.Where(p => p.InsuranceType == filter.InsuranceType);
        if (filter.Status is not null) query = query.Where(p => p.Status == filter.Status);
        if (filter.PaymentStatus is not null) query = query.Where(p => p.PaymentStatus == filter.PaymentStatus);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var t = filter.Search.Trim();
            query = query.Where(p =>
                (p.PolicyNumber != null && p.PolicyNumber.Contains(t)) ||
                p.Customer.FirstName.Contains(t) ||
                p.Customer.LastName.Contains(t) ||
                p.Customer.NationalCode.Contains(t) ||
                p.Store.StoreName.Contains(t) ||
                p.Imei1.Contains(t));
        }

        return query;
    }

    public async Task<PagedResult<InsuranceReportRow>> InsuranceAsync(InsuranceReportFilter filter, CancellationToken cancellationToken)
    {
        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);
        var query = InsuranceQuery(filter);
        var desc = !string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        query = (filter.SortBy?.ToLowerInvariant()) switch
        {
            "policynumber" => desc ? query.OrderByDescending(p => p.PolicyNumber) : query.OrderBy(p => p.PolicyNumber),
            "premium" => desc ? query.OrderByDescending(p => p.PremiumRial) : query.OrderBy(p => p.PremiumRial),
            _ => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<InsuranceReportRow>
        {
            Items = items.Select(Map).ToList(),
            Pagination = new PaginationMeta
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            }
        };
    }

    public async Task<IReadOnlyList<InsuranceReportRow>> InsuranceAllAsync(InsuranceReportFilter filter, CancellationToken cancellationToken)
    {
        var items = await InsuranceQuery(filter).OrderByDescending(p => p.CreatedAt).ToListAsync(cancellationToken);
        return items.Select(Map).ToList();
    }

    public static InsuranceReportRow Map(Domain.Entities.InsurancePolicy p)
    {
        var paid = p.Payments.OrderByDescending(x => x.PaidAt).FirstOrDefault(x => x.Status == PaymentStatus.Paid);
        return new InsuranceReportRow(
            p.Id,
            p.PolicyNumber,
            p.Store.StoreName,
            $"{p.Store.ManagerFirstName} {p.Store.ManagerLastName}",
            p.Store.Mobile1,
            p.Store.Province.Name,
            p.Store.City.Name,
            p.Store.Address,
            p.Customer.FirstName,
            p.Customer.LastName,
            p.Customer.NationalCode,
            p.Customer.BirthDate,
            p.Customer.Mobile,
            p.Customer.Address,
            p.Customer.PostalCode,
            p.InsuranceType,
            p.Brand.Name,
            p.Model.Name,
            p.MobilePriceRial,
            p.Imei1,
            p.Imei2,
            p.IssueDate,
            p.StartDate,
            p.EndDate,
            p.PremiumRial,
            p.CustomerChargedRial,
            StoreMarkup.Profit(p.CustomerChargedRial, p.PremiumRial),
            p.Status,
            p.PaymentStatus,
            paid?.TransactionId,
            paid?.TrackingCode ?? p.PaymentTrackingCode);
    }
}

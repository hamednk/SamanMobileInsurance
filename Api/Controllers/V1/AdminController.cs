using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Festivals;
using SamanMobileInsurance.Application.Admin;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Application.Reports;
using SamanMobileInsurance.Application.Stores;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = "Admin,Operator")]
[Route("api/v{version:apiVersion}/admin")]
public class AdminController : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<AdminDashboardDto>>> Dashboard(
        [FromServices] AdminDashboardService dashboard,
        CancellationToken cancellationToken) =>
        Success(await dashboard.GetAsync(cancellationToken));

    [HttpGet("stores")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminStoreListItem>>>> Stores(
        [FromServices] AdminStoreService stores,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? provinceId = null,
        [FromQuery] Guid? cityId = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var result = await stores.ListAsync(new StoreFilter(page, pageSize, search, provinceId, cityId, from, to, isActive, sortBy, sortDirection), cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("stores/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> Store(
        Guid id,
        [FromServices] AdminStoreService stores,
        CancellationToken cancellationToken) =>
        Success(await stores.GetAsync(id, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("stores")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> CreateStore(
        [FromBody] CreateStoreByAdminRequest request,
        [FromServices] AdminStoreService stores,
        CancellationToken cancellationToken) =>
        Success(await stores.CreateAsync(request, cancellationToken), "فروشگاه ایجاد شد.");

    [Authorize(Roles = "Admin")]
    [HttpPut("stores/{id:guid}")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> UpdateStore(
        Guid id,
        [FromBody] UpdateStoreRequest request,
        [FromServices] AdminStoreService stores,
        CancellationToken cancellationToken) =>
        Success(await stores.UpdateAsync(id, request, cancellationToken), "فروشگاه به‌روزرسانی شد.");

    [Authorize(Roles = "Admin")]
    [HttpPost("stores/{id:guid}/active")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> SetStoreActive(
        Guid id,
        [FromQuery] bool isActive,
        [FromServices] AdminStoreService stores,
        CancellationToken cancellationToken) =>
        Success(await stores.SetActiveAsync(id, isActive, cancellationToken));

    [HttpGet("users")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminUserListItem>>>> Users(
        [FromServices] AdminUserService users,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await users.ListAsync(page, pageSize, search, cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    public async Task<ActionResult<ApiResponse<AdminUserListItem>>> CreateUser(
        [FromBody] CreateUserRequest request,
        [FromServices] AdminUserService users,
        CancellationToken cancellationToken) =>
        Success(await users.CreateAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}/password")]
    public async Task<ActionResult<ApiResponse<object>>> SetUserPassword(
        Guid id,
        [FromBody] AdminSetPasswordRequest request,
        [FromServices] AdminUserService users,
        CancellationToken cancellationToken)
    {
        await users.SetPasswordAsync(id, request, cancellationToken);
        return Success<object>(null!, "رمز عبور کاربر به‌روزرسانی شد.");
    }
    [HttpGet("policies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InsuranceReportRow>>>> Policies(
        [FromServices] ReportService reports,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? cityId,
        [FromQuery] Guid? storeId,
        [FromQuery] InsuranceType? insuranceType,
        [FromQuery] PolicyStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        var result = await reports.InsuranceAsync(new InsuranceReportFilter(
            fromDate, toDate, provinceId, cityId, storeId, insuranceType, status, paymentStatus, search, page, pageSize, sortBy, sortDirection),
            cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("policies/{id:guid}")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Policy(
        Guid id,
        [FromServices] InsuranceService insurance,
        CancellationToken cancellationToken) =>
        Success(await insurance.GetAsync(id, cancellationToken));

    [HttpGet("policies/{policyId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> PolicyImage(
        Guid policyId,
        Guid imageId,
        [FromServices] InsuranceImageService images,
        CancellationToken cancellationToken)
    {
        var file = await images.OpenAsync(policyId, imageId, cancellationToken);
        return File(file.Stream, file.ContentType, file.FileName);
    }

    [HttpGet("customers")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CustomerListItem>>>> Customers(
        [FromServices] AdminQueryService query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await query.CustomersAsync(page, pageSize, search, cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("brands")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NamedItemDto>>>> Brands(
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.BrandsAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("brands")]
    public async Task<ActionResult<ApiResponse<NamedItemDto>>> CreateBrand(
        [FromBody] CreateNamedItemRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.CreateBrandAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("brands/{id:guid}")]
    public async Task<ActionResult<ApiResponse<NamedItemDto>>> UpdateBrand(
        Guid id,
        [FromBody] CreateNamedItemRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.UpdateBrandAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("brands/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBrand(
        Guid id,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteBrandAsync(id, cancellationToken);
        return Success<object>(null!, "برند حذف شد.");
    }

    [HttpGet("models")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModelItemDto>>>> Models(
        [FromServices] AdminCatalogService catalog,
        [FromQuery] Guid? brandId,
        CancellationToken cancellationToken) =>
        Success(await catalog.ModelsAsync(brandId, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("models")]
    public async Task<ActionResult<ApiResponse<ModelItemDto>>> CreateModel(
        [FromBody] CreateModelRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.CreateModelAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("models/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ModelItemDto>>> UpdateModel(
        Guid id,
        [FromBody] CreateNamedItemRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.UpdateModelAsync(id, request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpDelete("models/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModel(
        Guid id,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteModelAsync(id, cancellationToken);
        return Success<object>(null!, "مدل حذف شد.");
    }

    [HttpGet("provinces")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemNamed>>>> Provinces(
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.ProvincesAsync(cancellationToken));

    [HttpGet("cities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemNamed>>>> Cities(
        [FromServices] AdminCatalogService catalog,
        [FromQuery] Guid? provinceId,
        CancellationToken cancellationToken) =>
        Success(await catalog.CitiesAsync(provinceId, cancellationToken));

    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PaymentListItem>>>> Payments(
        [FromServices] AdminQueryService query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PaymentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await query.PaymentsAsync(page, pageSize, status, cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("settings")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SettingDto>>>> Settings(
        [FromServices] AdminSettingsService settings,
        CancellationToken cancellationToken) =>
        Success(await settings.ListAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("settings/{key}")]
    public async Task<ActionResult<ApiResponse<SettingDto>>> UpdateSetting(
        string key,
        [FromBody] UpdateSettingRequest request,
        [FromServices] AdminSettingsService settings,
        CancellationToken cancellationToken) =>
        Success(await settings.UpdateAsync(key, request.Value, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpGet("rates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RateDto>>>> Rates(
        [FromServices] AdminSettingsService settings,
        CancellationToken cancellationToken) =>
        Success(await settings.RatesAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("rates")]
    public async Task<ActionResult<ApiResponse<RateDto>>> CreateRate(
        [FromBody] UpsertRateRequest request,
        [FromServices] AdminSettingsService settings,
        CancellationToken cancellationToken) =>
        Success(await settings.CreateRateAsync(request, cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPut("rates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<RateDto>>> UpdateRate(
        Guid id,
        [FromBody] UpsertRateRequest request,
        [FromServices] AdminSettingsService settings,
        CancellationToken cancellationToken) =>
        Success(await settings.UpdateRateAsync(id, request, cancellationToken));

    [HttpGet("audit-logs")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditLogItem>>>> AuditLogs(
        [FromServices] AdminQueryService query,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await query.AuditLogsAsync(page, pageSize, search, cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("reports/insurance")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<InsuranceReportRow>>>> InsuranceReport(
        [FromServices] ReportService reports,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? cityId,
        [FromQuery] Guid? storeId,
        [FromQuery] InsuranceType? insuranceType,
        [FromQuery] PolicyStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await reports.InsuranceAsync(new InsuranceReportFilter(
            fromDate, toDate, provinceId, cityId, storeId, insuranceType, status, paymentStatus, search, page, pageSize, null, null),
            cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("reports/insurance/export")]
    public async Task<IActionResult> ExportInsurance(
        [FromServices] IExcelReportService excel,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? cityId,
        [FromQuery] Guid? storeId,
        [FromQuery] InsuranceType? insuranceType,
        [FromQuery] PolicyStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var bytes = await excel.ExportInsuranceAsync(new InsuranceReportFilter(
            fromDate, toDate, provinceId, cityId, storeId, insuranceType, status, paymentStatus, search, 1, 20, null, null),
            cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "insurance-report.xlsx");
    }

    [HttpGet("policies/export")]
    public async Task<IActionResult> ExportPolicies(
        [FromServices] IExcelReportService excel,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] Guid? provinceId,
        [FromQuery] Guid? cityId,
        [FromQuery] Guid? storeId,
        [FromQuery] InsuranceType? insuranceType,
        [FromQuery] PolicyStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var bytes = await excel.ExportInsuranceAsync(new InsuranceReportFilter(
            fromDate, toDate, provinceId, cityId, storeId, insuranceType, status, paymentStatus, search, 1, 20, null, null),
            cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "store-policies.xlsx");
    }

    [HttpGet("festivals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SalesFestivalDto>>>> Festivals(
        [FromServices] SalesFestivalService festivals,
        CancellationToken cancellationToken) =>
        Success(await festivals.ListAsync(cancellationToken));

    [Authorize(Roles = "Admin")]
    [HttpPost("festivals")]
    public async Task<ActionResult<ApiResponse<SalesFestivalDto>>> CreateFestival(
        [FromBody] UpsertSalesFestivalRequest request,
        [FromServices] SalesFestivalService festivals,
        CancellationToken cancellationToken) =>
        Success(await festivals.CreateAsync(request, cancellationToken), "جشنواره ثبت شد.");

    [Authorize(Roles = "Admin")]
    [HttpPut("festivals/{id:guid}")]
    public async Task<ActionResult<ApiResponse<SalesFestivalDto>>> UpdateFestival(
        Guid id,
        [FromBody] UpsertSalesFestivalRequest request,
        [FromServices] SalesFestivalService festivals,
        CancellationToken cancellationToken) =>
        Success(await festivals.UpdateAsync(id, request, cancellationToken), "جشنواره به‌روزرسانی شد.");

    [Authorize(Roles = "Admin")]
    [HttpGet("festivals/{id:guid}/progress")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FestivalStoreProgressDto>>>> FestivalProgress(
        Guid id,
        [FromServices] SalesFestivalService festivals,
        [FromQuery] bool onlyTargetReached = false,
        CancellationToken cancellationToken = default) =>
        Success(await festivals.GetStoreProgressAsync(id, onlyTargetReached, cancellationToken));

    [HttpDelete("festivals/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteFestival(
        Guid id,
        [FromServices] SalesFestivalService festivals,
        CancellationToken cancellationToken)
    {
        await festivals.DeleteAsync(id, cancellationToken);
        return Success<object>(null!, "جشنواره حذف شد.");
    }
}

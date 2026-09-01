using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Festivals;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Application.Lookups;
using SamanMobileInsurance.Application.Stores;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = "Store")]
[Route("api/v{version:apiVersion}/insurance")]
public class InsuranceController : ApiControllerBase
{
    private readonly InsuranceService _insurance;
    private readonly InsuranceImageService _images;
    private readonly PremiumCalculationService _premium;

    public InsuranceController(
        InsuranceService insurance,
        InsuranceImageService images,
        PremiumCalculationService premium)
    {
        _insurance = insurance;
        _images = images;
        _premium = premium;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PolicyListItemDto>>>> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        [FromQuery] InsuranceType? insuranceType = null,
        [FromQuery] PolicyStatus? status = null,
        [FromQuery] PaymentStatus? paymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _insurance.ListMineAsync(
            page, pageSize, search, fromDate, toDate, insuranceType, status, paymentStatus, cancellationToken);
        return Success(result.Items, pagination: result.Pagination);
    }

    [HttpGet("mine/export")]
    public async Task<IActionResult> ExportMine(
        [FromServices] IExcelReportService excel,
        [FromServices] ICurrentUser current,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] InsuranceType? insuranceType,
        [FromQuery] PolicyStatus? status,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] string? search,
        CancellationToken cancellationToken = default)
    {
        var storeId = current.StoreId ?? throw new ForbiddenAppException();
        var bytes = await excel.ExportInsuranceAsync(new InsuranceReportFilter(
            fromDate, toDate, null, null, storeId, insuranceType, status, paymentStatus, search, 1, 20, null, null),
            cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "store-policies.xlsx");
    }

    [HttpGet("renewals")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RenewalListItemDto>>>> Renewals(
        [FromQuery] string track = "expired",
        CancellationToken cancellationToken = default) =>
        Success(await _insurance.ListRenewalsAsync(track, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Get(Guid id, CancellationToken cancellationToken) =>
        Success(await _insurance.GetAsync(id, cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Create(
        [FromBody] CreatePolicyRequest request,
        [FromServices] IValidator<CreatePolicyRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Success(await _insurance.CreateDraftAsync(request, cancellationToken), "پیش‌نویس بیمه‌نامه ذخیره شد.");
    }

    [HttpPut("{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> UpdateDraft(
        Guid id,
        [FromBody] UpdatePolicyDraftRequest request,
        [FromServices] IValidator<UpdatePolicyDraftRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Success(await _insurance.UpdateDraftAsync(id, request, cancellationToken), "اطلاعات بیمه‌نامه به‌روزرسانی شد.");
    }

    [HttpPost("premium")]
    public async Task<ActionResult<ApiResponse<PremiumQuote>>> Premium(
        [FromBody] PremiumRequest request,
        [FromServices] IValidator<PremiumRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Success(await _premium.QuoteAsync(request.InsuranceType, request.MobilePriceRial, cancellationToken));
    }

    [HttpGet("imei/available")]
    public async Task<ActionResult<ApiResponse<ImeiAvailabilityDto>>> CheckImeiAvailability(
        [FromQuery] string imei1,
        [FromQuery] string? imei2,
        [FromQuery] Guid? excludePolicyId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imei1))
        {
            throw new ValidationAppException("سریال ۱ الزامی است.");
        }

        var normalizedImei2 = string.IsNullOrWhiteSpace(imei2) ? null : imei2.Trim();
        if (normalizedImei2 is not null && imei1 == normalizedImei2)
        {
            return Success(new ImeiAvailabilityDto(false, "سریال ۱ و سریال ۲ نباید یکسان باشند."));
        }

        var available = await _insurance.IsImeiAvailableAsync(imei1.Trim(), normalizedImei2, excludePolicyId, cancellationToken);
        return Success(available
            ? new ImeiAvailabilityDto(true)
            : new ImeiAvailabilityDto(false, "این IMEI دارای بیمه‌نامه فعال است و امکان ثبت بیمه جدید وجود ندارد."));
    }

    [HttpGet("customers/lookup")]
    public async Task<ActionResult<ApiResponse<CustomerInput?>>> LookupCustomer(
        [FromQuery] string? nationalCode,
        [FromQuery] string? mobile,
        CancellationToken cancellationToken)
    {
        var found = await _insurance.FindCustomerAsync(nationalCode ?? "", mobile ?? "", cancellationToken);
        if (found is null)
        {
            return Success<CustomerInput?>(null);
        }

        return Success<CustomerInput?>(new CustomerInput(
            found.FirstName, found.LastName, found.NationalCode, found.BirthDate,
            found.Mobile, found.Address, found.PostalCode));
    }

    [HttpPost("{id:guid}/images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> UploadImage(
        Guid id,
        [FromForm] ImageType imageType,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await _images.UploadAsync(id, imageType, stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        return Success(result, "تصویر با موفقیت بارگذاری شد.");
    }

    [HttpGet("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DownloadImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var file = await _images.OpenAsync(id, imageId, cancellationToken);
        return File(file.Stream, file.ContentType, file.FileName);
    }

    [HttpPost("{id:guid}/renew")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Renew(Guid id, CancellationToken cancellationToken) =>
        Success(await _insurance.RenewAsync(id, cancellationToken), "تمدید بیمه‌نامه ایجاد شد. ادامه پرداخت را انجام دهید.");

    [HttpPut("{id:guid}/customer-charged")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> SetCustomerCharged(
        Guid id,
        [FromBody] SetCustomerChargedRequest request,
        [FromServices] IValidator<SetCustomerChargedRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        return Success(await _insurance.SetCustomerChargedAsync(id, request.CustomerChargedRial, cancellationToken), "مبلغ دریافتی از مشتری ذخیره شد.");
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<PolicyDto>>> Cancel(Guid id, CancellationToken cancellationToken) =>
        Success(await _insurance.CancelAsync(id, cancellationToken), "بیمه‌نامه لغو شد.");
}

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = "Store")]
[Route("api/v{version:apiVersion}/store")]
public class StoreDashboardController : ApiControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<StoreDashboardDto>>> Dashboard(
        [FromServices] StoreDashboardService dashboard,
        CancellationToken cancellationToken) =>
        Success(await dashboard.GetAsync(cancellationToken));

    [HttpGet("festival")]
    public async Task<ActionResult<ApiResponse<StoreFestivalStatusDto>>> Festival(
        [FromServices] SalesFestivalService festivals,
        CancellationToken cancellationToken) =>
        Success(await festivals.GetStoreStatusAsync(cancellationToken));

    [HttpGet("performance")]
    public async Task<ActionResult<ApiResponse<StorePerformanceReportDto>>> Performance(
        [FromServices] StorePerformanceService performance,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        Success(await performance.GetAsync(from, to, cancellationToken));
}

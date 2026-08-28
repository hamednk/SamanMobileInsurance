using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Stores;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/stores")]
public class StoresController : ApiControllerBase
{
    private readonly StoreService _stores;
    private readonly ICaptchaService _captcha;

    public StoresController(StoreService stores, ICaptchaService captcha)
    {
        _stores = stores;
        _captcha = captcha;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> Register(
        [FromBody] RegisterStoreRequest request,
        [FromServices] IValidator<RegisterStoreRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        _captcha.Validate(request.CaptchaId, request.CaptchaCode);
        var result = await _stores.RegisterAsync(request, cancellationToken);
        return Success(result, "ثبت‌نام فروشگاه با موفقیت انجام شد.");
    }

    [Authorize(Roles = "Store")]
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<StoreProfileDto>>> Me(
        [FromServices] ICurrentUser current,
        CancellationToken cancellationToken) =>
        Success(await _stores.GetCurrentAsync(current, cancellationToken));
}

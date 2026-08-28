using Asp.Versioning;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Auth;
using SamanMobileInsurance.Application.Common;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AuthService _auth;
    private readonly ICaptchaService _captcha;

    public AuthController(AuthService auth, ICaptchaService captcha)
    {
        _auth = auth;
        _captcha = captcha;
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpGet("captcha")]
    public ActionResult<ApiResponse<CaptchaChallengeDto>> Captcha() =>
        Success(_captcha.Create());

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthTokensDto>>> Login(
        [FromBody] LoginRequest request,
        [FromServices] IValidator<LoginRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        _captcha.Validate(request.CaptchaId, request.CaptchaCode);
        var result = await _auth.LoginAsync(request, ClientIp, cancellationToken);
        SetRefreshCookie(result.RefreshToken);
        return Success(result, "ورود با موفقیت انجام شد.");
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthTokensDto>>> Refresh(
        [FromBody] RefreshRequest? request,
        CancellationToken cancellationToken)
    {
        var token = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        var result = await _auth.RefreshAsync(token ?? string.Empty, ClientIp, cancellationToken);
        SetRefreshCookie(result.RefreshToken);
        return Success(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshRequest? request,
        CancellationToken cancellationToken)
    {
        var token = request?.RefreshToken ?? Request.Cookies["refreshToken"];
        await _auth.LogoutAsync(token, cancellationToken);
        Response.Cookies.Delete("refreshToken");
        return Success<object>(null!, "خروج انجام شد.");
    }

    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<ForgotPasswordResultDto>>> Forgot(
        [FromBody] ForgotPasswordRequest request,
        [FromServices] IValidator<ForgotPasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        var result = await _auth.ForgotPasswordAsync(request, cancellationToken);
        return Success(result, "نام کاربری تأیید شد. رمز جدید را وارد کنید.");
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> Reset(
        [FromBody] ResetPasswordRequest request,
        [FromServices] IValidator<ResetPasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        await validator.EnsureValidAsync(request, cancellationToken);
        await _auth.ResetPasswordAsync(request, cancellationToken);
        return Success<object>(null!, "رمز عبور با موفقیت تغییر کرد.");
    }

    private void SetRefreshCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(14)
        });
    }
}

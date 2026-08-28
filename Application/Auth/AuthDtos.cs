using FluentValidation;
using SamanMobileInsurance.Application.Validation;

namespace SamanMobileInsurance.Application.Auth;

public record LoginRequest(string Username, string Password, Guid CaptchaId, string CaptchaCode);
public record RefreshRequest(string? RefreshToken);
public record ForgotPasswordRequest(string Username);
public record ForgotPasswordResultDto(string ResetToken);
public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);

public record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string Role,
    string Username,
    Guid? StoreId);

public class LoginRequestValidator : FluentValidation.AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("نام کاربری الزامی است.").MaximumLength(64);
        RuleFor(x => x.Password).NotEmpty().WithMessage("رمز عبور الزامی است.");
        RuleFor(x => x.CaptchaId).NotEmpty().WithMessage("کد امنیتی الزامی است.");
        RuleFor(x => x.CaptchaCode).NotEmpty().WithMessage("کد امنیتی الزامی است.").MaximumLength(12);
    }
}

public class ResetPasswordRequestValidator : FluentValidation.AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("توکن بازیابی الزامی است.");
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.");
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("تکرار رمز عبور مطابقت ندارد.");
    }
}

public class ForgotPasswordRequestValidator : FluentValidation.AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("نام کاربری الزامی است.");
    }
}

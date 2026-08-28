using FluentValidation;
using SamanMobileInsurance.Application.Validation;

namespace SamanMobileInsurance.Application.Stores;

public record RegisterStoreRequest(
    string StoreName,
    string ManagerFirstName,
    string ManagerLastName,
    string NationalCode,
    DateOnly BirthDate,
    string Mobile1,
    string? Mobile2,
    Guid ProvinceId,
    Guid CityId,
    string Address,
    string PostalCode,
    string Username,
    string Password,
    Guid CaptchaId,
    string CaptchaCode);

public record StoreProfileDto(
    Guid Id,
    string StoreName,
    string ManagerFirstName,
    string ManagerLastName,
    string NationalCode,
    DateOnly BirthDate,
    string Mobile1,
    string? Mobile2,
    Guid ProvinceId,
    string ProvinceName,
    Guid CityId,
    string CityName,
    string Address,
    string PostalCode,
    string Username,
    bool IsActive,
    DateTimeOffset CreatedAt);

public class RegisterStoreRequestValidator : AbstractValidator<RegisterStoreRequest>
{
    public RegisterStoreRequestValidator()
    {
        RuleFor(x => x.StoreName).NotEmpty().WithMessage("نام فروشگاه الزامی است.").MaximumLength(200);
        RuleFor(x => x.ManagerFirstName).NotEmpty().WithMessage("نام مدیر الزامی است.").MaximumLength(80);
        RuleFor(x => x.ManagerLastName).NotEmpty().WithMessage("نام خانوادگی مدیر الزامی است.").MaximumLength(80);
        RuleFor(x => x.NationalCode)
            .NotEmpty().WithMessage("کد ملی الزامی است.")
            .Must(IranianNationalCode.IsValid).WithMessage("کد ملی معتبر نیست.");
        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithMessage("تاریخ تولد نامعتبر است.");
        RuleFor(x => x.Mobile1)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Must(IranianMobile.IsValid).WithMessage("شماره موبایل معتبر نیست.");
        RuleFor(x => x.Mobile2)
            .Must(v => string.IsNullOrWhiteSpace(v) || IranianMobile.IsValid(v))
            .WithMessage("شماره موبایل دوم معتبر نیست.");
        RuleFor(x => x.ProvinceId).NotEmpty().WithMessage("استان الزامی است.");
        RuleFor(x => x.CityId).NotEmpty().WithMessage("شهر الزامی است.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("آدرس الزامی است.").MaximumLength(500);
        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("کد پستی الزامی است.")
            .Must(IranianPostalCode.IsValid).WithMessage("کد پستی باید ۱۰ رقم باشد.");
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام کاربری الزامی است.")
            .MinimumLength(4).WithMessage("نام کاربری باید حداقل ۴ کاراکتر باشد.")
            .MaximumLength(64)
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("نام کاربری فقط می‌تواند شامل حروف انگلیسی، عدد و . _ - باشد.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.");
        RuleFor(x => x.CaptchaId).NotEmpty().WithMessage("کد امنیتی الزامی است.");
        RuleFor(x => x.CaptchaCode).NotEmpty().WithMessage("کد امنیتی الزامی است.").MaximumLength(12);
    }
}

using FluentValidation;
using SamanMobileInsurance.Application.Validation;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public record CustomerInput(
    string FirstName,
    string LastName,
    string NationalCode,
    DateOnly BirthDate,
    string Mobile,
    string Address,
    string PostalCode);

public record CreatePolicyRequest(
    InsuranceType InsuranceType,
    CustomerInput Customer,
    Guid BrandId,
    Guid ModelId,
    decimal MobilePriceRial,
    string Imei1,
    string? Imei2,
    DateTimeOffset? StartDate);

public record PremiumRequest(InsuranceType InsuranceType, decimal MobilePriceRial);

public record PolicyDto(
    Guid Id,
    string? PolicyNumber,
    InsuranceType InsuranceType,
    PolicyStatus Status,
    PaymentStatus PaymentStatus,
    decimal MobilePriceRial,
    decimal PremiumRial,
    string Imei1,
    string? Imei2,
    DateTimeOffset StartDate,
    DateTimeOffset? EndDate,
    DateTimeOffset? IssueDate,
    DateTimeOffset CreatedAt,
    Guid StoreId,
    string StoreName,
    Guid CustomerId,
    string CustomerFirstName,
    string CustomerLastName,
    string CustomerNationalCode,
    string CustomerMobile,
    string CustomerAddress,
    string CustomerPostalCode,
    DateOnly CustomerBirthDate,
    Guid BrandId,
    string BrandName,
    Guid ModelId,
    string ModelName,
    string? PaymentTrackingCode,
    Guid? RenewedFromPolicyId,
    bool CanRenew,
    IReadOnlyList<PolicyImageDto> Images);

public record PolicyImageDto(Guid Id, ImageType ImageType, string FileName, DateTimeOffset UploadedAt);

public record PolicyListItemDto(
    Guid Id,
    string? PolicyNumber,
    InsuranceType InsuranceType,
    PolicyStatus Status,
    PaymentStatus PaymentStatus,
    decimal PremiumRial,
    string CustomerName,
    string BrandName,
    string ModelName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssueDate,
    DateTimeOffset? EndDate,
    Guid? RenewedFromPolicyId,
    bool CanRenew);

public record RenewalListItemDto(
    Guid Id,
    string? PolicyNumber,
    InsuranceType InsuranceType,
    PolicyStatus Status,
    PaymentStatus PaymentStatus,
    decimal PremiumRial,
    string CustomerName,
    string BrandName,
    string ModelName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssueDate,
    DateTimeOffset? EndDate,
    Guid? RenewedFromPolicyId,
    bool CanRenew,
    /// <summary>Expired = منقضی‌شده / قابل تمدید؛ Renewed = تمدید ثبت‌شده</summary>
    string RenewalTrack);


public class CustomerInputValidator : AbstractValidator<CustomerInput>
{
    public CustomerInputValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("نام الزامی است.").MaximumLength(80);
        RuleFor(x => x.LastName).NotEmpty().WithMessage("نام خانوادگی الزامی است.").MaximumLength(80);
        RuleFor(x => x.NationalCode)
            .Must(IranianNationalCode.IsValid).WithMessage("کد ملی معتبر نیست.");
        RuleFor(x => x.BirthDate).LessThan(DateOnly.FromDateTime(DateTime.UtcNow.Date));
        RuleFor(x => x.Mobile).Must(IranianMobile.IsValid).WithMessage("شماره موبایل معتبر نیست.");
        RuleFor(x => x.Address).NotEmpty().WithMessage("آدرس الزامی است.").MaximumLength(500);
        RuleFor(x => x.PostalCode).Must(IranianPostalCode.IsValid).WithMessage("کد پستی باید ۱۰ رقم باشد.");
    }
}

public class CreatePolicyRequestValidator : AbstractValidator<CreatePolicyRequest>
{
    public CreatePolicyRequestValidator()
    {
        RuleFor(x => x.Customer).SetValidator(new CustomerInputValidator());
        RuleFor(x => x.BrandId).NotEmpty();
        RuleFor(x => x.ModelId).NotEmpty();
        RuleFor(x => x.MobilePriceRial).GreaterThan(0).WithMessage("قیمت موبایل باید بزرگ‌تر از صفر باشد.");
        RuleFor(x => x.Imei1)
            .Must(ImeiValidator.IsValid).WithMessage("IMEI 1 معتبر نیست.");
        RuleFor(x => x.Imei2)
            .Must(v => string.IsNullOrWhiteSpace(v) || ImeiValidator.IsValid(v))
            .WithMessage("IMEI 2 معتبر نیست.");
        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.Imei2) || x.Imei1 != x.Imei2)
            .WithMessage("IMEI 1 و IMEI 2 نباید یکسان باشند.");
        RuleFor(x => x.InsuranceType).IsInEnum();
        When(x => x.InsuranceType == InsuranceType.New, () =>
        {
            RuleFor(x => x.StartDate).NotNull().WithMessage("برای گوشی آکبند، تاریخ شروع بیمه‌نامه الزامی است.");
        });
    }
}

public class PremiumRequestValidator : AbstractValidator<PremiumRequest>
{
    public PremiumRequestValidator()
    {
        RuleFor(x => x.MobilePriceRial).GreaterThan(0);
        RuleFor(x => x.InsuranceType).IsInEnum();
    }
}

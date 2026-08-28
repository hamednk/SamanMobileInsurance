using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(User user, Guid? storeId);
    string CreateRefreshToken();
    string HashToken(string token);
}

public interface IPasswordHasherService
{
    string Hash(string password);
    bool Verify(string hash, string password);
}

public interface IAuditLogger
{
    Task LogAsync(string action, string entityName, string? entityId, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string contentType, string folder, CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

public record StoredFile(string Path, string FileName, string ContentType);

public interface IImageProcessor
{
    Task<ProcessedImage> ProcessAsync(Stream input, string contentType, CancellationToken cancellationToken = default);
}

public record ProcessedImage(Stream Content, string ContentType, string Extension);

public interface INotificationService
{
    Task SendPasswordResetAsync(string destination, string resetToken, CancellationToken cancellationToken = default);
}

public record CaptchaChallengeDto(Guid CaptchaId, string ImageSvg);

public interface ICaptchaService
{
    CaptchaChallengeDto Create();
    void Validate(Guid captchaId, string? code);
}

public record PaymentInitResult(string Authority, string RedirectUrl);

public record PaymentVerifyResult(bool IsSuccess, string? TrackingCode, string? TransactionId, string? Message);

public interface IPaymentGateway
{
    PaymentGatewayType GatewayType { get; }
    Task<PaymentInitResult> InitiateAsync(Guid paymentId, decimal amountRial, string description, string callbackUrl, CancellationToken cancellationToken = default);
    Task<PaymentVerifyResult> VerifyAsync(string authority, decimal amountRial, CancellationToken cancellationToken = default);
}

using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Insurance;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Payments;

public record PaymentInitDto(Guid PaymentId, decimal AmountRial, string RedirectUrl, string Authority);

public record PaymentCallbackRequest(string Authority, string? Status);

public class PaymentService
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _gateway;
    private readonly ICurrentUser _current;
    private readonly PolicyIssuanceService _issuance;
    private readonly IAuditLogger _audit;
    private readonly string _callbackBaseUrl;
    private readonly string _frontendBaseUrl;

    public PaymentService(
        IApplicationDbContext db,
        IPaymentGateway gateway,
        ICurrentUser current,
        PolicyIssuanceService issuance,
        IAuditLogger audit,
        PaymentOptions options)
    {
        _db = db;
        _gateway = gateway;
        _current = current;
        _issuance = issuance;
        _audit = audit;
        _callbackBaseUrl = options.CallbackBaseUrl.TrimEnd('/');
        _frontendBaseUrl = options.FrontendBaseUrl.TrimEnd('/');
    }

    public async Task<PaymentInitDto> InitiateAsync(Guid policyId, CancellationToken cancellationToken)
    {
        var policy = await _db.InsurancePolicies
            .Include(p => p.Images)
            .Include(p => p.Customer)
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken)
            ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

        if (_current.IsStore && policy.StoreId != _current.StoreId)
        {
            throw new ForbiddenAppException();
        }

        if (policy.Status is PolicyStatus.Issued or PolicyStatus.Paid)
        {
            throw new BusinessRuleException("این بیمه‌نامه قبلاً پرداخت شده است.");
        }

        if (policy.Status != PolicyStatus.AwaitingPayment)
        {
            throw new BusinessRuleException("بیمه‌نامه برای پرداخت آماده نیست. ابتدا تصاویر را تکمیل کنید.");
        }

        if (policy.Images.All(i => i.ImageType != ImageType.Front) ||
            policy.Images.All(i => i.ImageType != ImageType.Back))
        {
            throw new BusinessRuleException("تصویر روی و پشت گوشی الزامی است.");
        }

        var pending = await _db.Payments
            .Where(p => p.PolicyId == policy.Id && p.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);
        foreach (var old in pending)
        {
            old.Status = PaymentStatus.Cancelled;
            old.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var payment = new Payment
        {
            PolicyId = policy.Id,
            AmountRial = policy.PremiumRial,
            PaymentGateway = _gateway.GatewayType,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        var callback = $"{_callbackBaseUrl}/api/v1/payments/callback";
        var init = await _gateway.InitiateAsync(
            payment.Id,
            payment.AmountRial,
            $"بیمه موبایل {policy.Customer.LastName}",
            callback,
            cancellationToken);

        payment.Authority = init.Authority;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("payment-init", nameof(Payment), payment.Id.ToString(), cancellationToken);

        return new PaymentInitDto(payment.Id, payment.AmountRial, init.RedirectUrl, init.Authority);
    }

    public async Task<string> HandleCallbackAsync(string authority, string? status, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments
            .Include(p => p.Policy)
            .FirstOrDefaultAsync(p => p.Authority == authority, cancellationToken)
            ?? throw new NotFoundException("پرداخت یافت نشد.");

        var successUrl = $"{_frontendBaseUrl}/insurance/{payment.PolicyId}/success";
        var failUrl = $"{_frontendBaseUrl}/insurance/{payment.PolicyId}/payment?failed=1";

        if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return failUrl;
        }

        try
        {
            await VerifyAndIssueAsync(payment, cancellationToken);
            return successUrl;
        }
        catch
        {
            return failUrl;
        }
    }

    public async Task VerifyAndIssueAsync(Guid paymentId, CancellationToken cancellationToken)
    {
        var payment = await _db.Payments.Include(p => p.Policy)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken)
            ?? throw new NotFoundException("پرداخت یافت نشد.");
        await VerifyAndIssueAsync(payment, cancellationToken);
    }

    private async Task VerifyAndIssueAsync(Payment payment, CancellationToken cancellationToken)
    {
        if (payment.Status == PaymentStatus.Paid && payment.Policy.Status == PolicyStatus.Issued)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(payment.Authority))
        {
            throw new BusinessRuleException("شناسه درگاه نامعتبر است.");
        }

        var verify = await _gateway.VerifyAsync(payment.Authority, payment.AmountRial, cancellationToken);
        if (!verify.IsSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            throw new BusinessRuleException(verify.Message ?? "تأیید پرداخت ناموفق بود.");
        }

        await _db.ExecuteInTransactionAsync(async ct =>
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    var policy = await _db.InsurancePolicies
                        .FirstOrDefaultAsync(p => p.Id == payment.PolicyId, ct)
                        ?? throw new NotFoundException("بیمه‌نامه یافت نشد.");

                    if (policy.Status == PolicyStatus.Issued)
                    {
                        return;
                    }

                    PolicyStateMachine.Ensure(policy.Status, PolicyStatus.Paid);
                    policy.Status = PolicyStatus.Paid;
                    policy.PaymentStatus = PaymentStatus.Paid;
                    policy.PaymentTrackingCode = verify.TrackingCode;
                    policy.UpdatedAt = DateTimeOffset.UtcNow;

                    payment.Status = PaymentStatus.Paid;
                    payment.PaidAt = DateTimeOffset.UtcNow;
                    payment.TrackingCode = verify.TrackingCode;
                    payment.TransactionId = verify.TransactionId;
                    payment.UpdatedAt = DateTimeOffset.UtcNow;

                    await _issuance.IssueAsync(policy, ct);
                    await _db.SaveChangesAsync(ct);
                    return;
                }
                catch (DbUpdateConcurrencyException) when (attempt < 2)
                {
                    foreach (var entry in _db.ChangeTracker.Entries().ToList())
                    {
                        await entry.ReloadAsync(ct);
                    }
                }
            }
        }, cancellationToken);
        await _audit.LogAsync("payment-verified", nameof(Payment), payment.Id.ToString(), cancellationToken);
    }
}

public class PaymentOptions
{
    public string CallbackBaseUrl { get; set; } = "http://localhost:5290";
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
}

using SamanMobileInsurance.Domain.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Domain.Entities;

public class Payment : BaseEntity
{
    public Guid PolicyId { get; set; }
    public decimal AmountRial { get; set; }
    public PaymentGatewayType PaymentGateway { get; set; } = PaymentGatewayType.Mock;
    public string? TransactionId { get; set; }
    public string? TrackingCode { get; set; }
    public string? Authority { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset? PaidAt { get; set; }

    public InsurancePolicy Policy { get; set; } = null!;
}

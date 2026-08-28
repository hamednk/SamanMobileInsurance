using SamanMobileInsurance.Domain.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Domain.Entities;

public class InsurancePolicy : BaseEntity
{
    public string? PolicyNumber { get; set; }
    public Guid StoreId { get; set; }
    public Guid CustomerId { get; set; }
    public InsuranceType InsuranceType { get; set; }
    public Guid BrandId { get; set; }
    public Guid ModelId { get; set; }
    public decimal MobilePriceRial { get; set; }
    public decimal PremiumRial { get; set; }
    public string Imei1 { get; set; } = string.Empty;
    public string? Imei2 { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? IssueDate { get; set; }
    public PolicyStatus Status { get; set; } = PolicyStatus.Draft;
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? PaymentTrackingCode { get; set; }
    public Guid? RenewedFromPolicyId { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public Store Store { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public MobileBrand Brand { get; set; } = null!;
    public MobileModel Model { get; set; } = null!;
    public InsurancePolicy? RenewedFromPolicy { get; set; }
    public ICollection<InsuranceImage> Images { get; set; } = new List<InsuranceImage>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

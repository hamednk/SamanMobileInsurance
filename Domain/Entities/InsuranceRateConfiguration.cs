using SamanMobileInsurance.Domain.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Domain.Entities;

public class InsuranceRateConfiguration : BaseEntity
{
    public InsuranceType InsuranceType { get; set; }
    public decimal MinPriceRial { get; set; }
    public decimal MaxPriceRial { get; set; }
    public decimal RatePercent { get; set; }
    public bool IsActive { get; set; } = true;
}

using SamanMobileInsurance.Domain.Common;

namespace SamanMobileInsurance.Domain.Entities;

public class City : BaseEntity
{
    public Guid ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Province Province { get; set; } = null!;
}

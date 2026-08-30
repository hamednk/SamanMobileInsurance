using SamanMobileInsurance.Domain.Common;

namespace SamanMobileInsurance.Domain.Entities;

public class MobileModel : BaseEntity, ISoftDeletable
{
    public Guid BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    /// <summary>User who added this model (store user). Catalog/seed models are null.</summary>
    public Guid? CreatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public MobileBrand Brand { get; set; } = null!;
}

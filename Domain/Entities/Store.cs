using SamanMobileInsurance.Domain.Common;

namespace SamanMobileInsurance.Domain.Entities;

public class Store : BaseEntity, ISoftDeletable
{
    public string StoreName { get; set; } = string.Empty;
    public string ManagerFirstName { get; set; } = string.Empty;
    public string ManagerLastName { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
    public DateOnly BirthDate { get; set; }
    public string Mobile1 { get; set; } = string.Empty;
    public string? Mobile2 { get; set; }
    public Guid ProvinceId { get; set; }
    public Guid CityId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public Province Province { get; set; } = null!;
    public City City { get; set; } = null!;
    public ICollection<InsurancePolicy> Policies { get; set; } = new List<InsurancePolicy>();
}

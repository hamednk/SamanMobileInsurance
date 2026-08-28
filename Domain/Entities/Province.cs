using SamanMobileInsurance.Domain.Common;

namespace SamanMobileInsurance.Domain.Entities;

public class Province : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<City> Cities { get; set; } = new List<City>();
}

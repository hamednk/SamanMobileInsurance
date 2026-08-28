using SamanMobileInsurance.Domain.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Domain.Entities;

public class InsuranceImage : BaseEntity
{
    public Guid PolicyId { get; set; }
    public ImageType ImageType { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }

    public InsurancePolicy Policy { get; set; } = null!;
}

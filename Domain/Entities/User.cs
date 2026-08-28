using SamanMobileInsurance.Domain.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Domain.Entities;

public class User : BaseEntity, ISoftDeletable
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public Store? Store { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}

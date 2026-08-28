using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    UserRole? Role { get; }
    Guid? StoreId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsOperator { get; }
    bool IsStore { get; }
}

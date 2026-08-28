using System.Security.Claims;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Api.Auth;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub"), out var id)
            ? id
            : null;

    public string? Username => Principal?.Identity?.Name;

    public UserRole? Role =>
        Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;

    public Guid? StoreId =>
        Guid.TryParse(Principal?.FindFirstValue("store_id"), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsOperator => Role == UserRole.Operator;
    public bool IsStore => Role == UserRole.Store;
}

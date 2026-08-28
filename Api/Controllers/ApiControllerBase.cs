using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Common;

namespace SamanMobileInsurance.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult<ApiResponse<T>> Success<T>(T data, string? message = null, PaginationMeta? pagination = null) =>
        Ok(ApiResponse<T>.Ok(data, message, pagination));

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}

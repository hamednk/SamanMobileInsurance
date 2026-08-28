using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Lookups;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/lookups")]
public class LookupsController : ApiControllerBase
{
    private readonly LookupService _lookups;

    public LookupsController(LookupService lookups) => _lookups = lookups;

    [HttpGet("provinces")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> Provinces(CancellationToken cancellationToken) =>
        Success(await _lookups.ProvincesAsync(cancellationToken));

    [HttpGet("cities")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CityLookupDto>>>> Cities(
        [FromQuery] Guid? provinceId,
        CancellationToken cancellationToken) =>
        Success(await _lookups.CitiesAsync(provinceId, cancellationToken));

    [HttpGet("brands")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> Brands(CancellationToken cancellationToken) =>
        Success(await _lookups.BrandsAsync(cancellationToken));

    [HttpGet("models")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> Models(
        [FromQuery] Guid brandId,
        CancellationToken cancellationToken) =>
        Success(await _lookups.ModelsAsync(brandId, cancellationToken));
}

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Admin;
using SamanMobileInsurance.Application.Common;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = "Store")]
[Route("api/v{version:apiVersion}/store/catalog")]
public class StoreCatalogController : ApiControllerBase
{
    [HttpGet("brands")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NamedItemDto>>>> Brands(
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.BrandsAsync(cancellationToken));

    [HttpGet("models")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ModelItemDto>>>> Models(
        [FromServices] AdminCatalogService catalog,
        [FromQuery] Guid? brandId,
        CancellationToken cancellationToken) =>
        Success(await catalog.ModelsAsync(brandId, cancellationToken));

    [HttpPost("models")]
    public async Task<ActionResult<ApiResponse<ModelItemDto>>> CreateModel(
        [FromBody] CreateModelRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.CreateModelAsync(request, cancellationToken), "مدل اضافه شد.");

    [HttpPut("models/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ModelItemDto>>> UpdateModel(
        Guid id,
        [FromBody] CreateNamedItemRequest request,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken) =>
        Success(await catalog.UpdateModelAsync(id, request, cancellationToken), "مدل به‌روزرسانی شد.");

    [HttpDelete("models/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteModel(
        Guid id,
        [FromServices] AdminCatalogService catalog,
        CancellationToken cancellationToken)
    {
        await catalog.DeleteModelAsync(id, cancellationToken);
        return Success<object>(null!, "مدل حذف شد.");
    }
}

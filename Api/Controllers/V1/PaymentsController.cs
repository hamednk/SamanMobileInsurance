using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Application.Payments;

namespace SamanMobileInsurance.Api.Controllers.V1;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/payments")]
public class PaymentsController : ApiControllerBase
{
    private readonly PaymentService _payments;

    public PaymentsController(PaymentService payments) => _payments = payments;

    [Authorize(Roles = "Store")]
    [HttpPost("init/{policyId:guid}")]
    public async Task<ActionResult<ApiResponse<PaymentInitDto>>> Init(Guid policyId, CancellationToken cancellationToken) =>
        Success(await _payments.InitiateAsync(policyId, cancellationToken), "در حال انتقال به درگاه...");

    [AllowAnonymous]
    [HttpGet("callback")]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string authority,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var url = await _payments.HandleCallbackAsync(authority, status, cancellationToken);
        return Redirect(url);
    }
}

[ApiController]
[ApiVersion(1.0)]
[Authorize(Roles = "Store")]
[Route("api/v{version:apiVersion}/insurance")]
public class InsurancePaymentController : ApiControllerBase
{
    [HttpPost("{id:guid}/payment/init")]
    public async Task<ActionResult<ApiResponse<PaymentInitDto>>> Init(
        Guid id,
        [FromServices] PaymentService payments,
        CancellationToken cancellationToken) =>
        Success(await payments.InitiateAsync(id, cancellationToken));
}

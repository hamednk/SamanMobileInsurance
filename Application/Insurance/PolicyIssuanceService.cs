using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public class PolicyIssuanceService
{
    private readonly IApplicationDbContext _db;

    public PolicyIssuanceService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task IssueAsync(InsurancePolicy policy, CancellationToken cancellationToken)
    {
        if (policy.Status == PolicyStatus.Issued && !string.IsNullOrWhiteSpace(policy.PolicyNumber))
        {
            return;
        }

        PolicyStateMachine.Ensure(policy.Status, PolicyStatus.Issued);

        var year = IranDateTime.JalaliYear(DateTimeOffset.UtcNow);
        var prefix = $"SM-{year}-";

        var last = await _db.InsurancePolicies
            .Where(p => p.PolicyNumber != null && p.PolicyNumber.StartsWith(prefix))
            .OrderByDescending(p => p.PolicyNumber)
            .Select(p => p.PolicyNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last))
        {
            var seqPart = last.Split('-').LastOrDefault();
            if (int.TryParse(seqPart, out var parsed))
            {
                next = parsed + 1;
            }
        }

        policy.PolicyNumber = $"{prefix}{next:000000}";
        policy.Status = PolicyStatus.Issued;
        policy.IssueDate = DateTimeOffset.UtcNow;
        policy.EndDate ??= policy.StartDate.AddYears(1);
        policy.PaymentStatus = PaymentStatus.Paid;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        if (policy.RenewedFromPolicyId is Guid previousId)
        {
            var previous = await _db.InsurancePolicies.FirstOrDefaultAsync(p => p.Id == previousId, cancellationToken);
            if (previous is not null && previous.Status == PolicyStatus.Issued)
            {
                previous.Status = PolicyStatus.Expired;
                previous.EndDate ??= DateTimeOffset.UtcNow;
                previous.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}
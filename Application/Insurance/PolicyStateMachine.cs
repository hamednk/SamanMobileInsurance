using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Insurance;

public static class PolicyStateMachine
{
    private static readonly Dictionary<PolicyStatus, HashSet<PolicyStatus>> Allowed = new()
    {
        [PolicyStatus.Draft] = [PolicyStatus.AwaitingImages, PolicyStatus.Cancelled],
        [PolicyStatus.AwaitingImages] = [PolicyStatus.AwaitingPayment, PolicyStatus.Cancelled],
        [PolicyStatus.AwaitingPayment] = [PolicyStatus.Paid, PolicyStatus.Cancelled],
        [PolicyStatus.Paid] = [PolicyStatus.Issued],
        [PolicyStatus.Issued] = [PolicyStatus.Expired],
        [PolicyStatus.Cancelled] = [],
        [PolicyStatus.Expired] = []
    };

    public static bool CanTransition(PolicyStatus from, PolicyStatus to) =>
        Allowed.TryGetValue(from, out var next) && next.Contains(to);

    public static void Ensure(PolicyStatus from, PolicyStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new BusinessRuleException($"تغییر وضعیت از {from} به {to} مجاز نیست.");
        }
    }
}

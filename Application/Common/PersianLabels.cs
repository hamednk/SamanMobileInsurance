using SamanMobileInsurance.Domain.Enums;

namespace SamanMobileInsurance.Application.Common;

public static class PersianLabels
{
    public static string ForPolicyStatus(PolicyStatus status) => status switch
    {
        PolicyStatus.Draft => "پیش‌نویس",
        PolicyStatus.AwaitingImages => "در انتظار تصویر",
        PolicyStatus.AwaitingPayment => "در انتظار پرداخت",
        PolicyStatus.Paid => "ثبت‌شده",
        PolicyStatus.Issued => "صادر شده",
        PolicyStatus.Cancelled => "لغو شده",
        PolicyStatus.Expired => "منقضی‌شده",
        _ => status.ToString()
    };

    public static string ForPaymentStatus(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "در انتظار",
        PaymentStatus.Paid => "پرداخت‌شده",
        PaymentStatus.Failed => "ناموفق",
        PaymentStatus.Cancelled => "لغو شده",
        _ => status.ToString()
    };

    public static string ForInsuranceType(InsuranceType type) => type switch
    {
        InsuranceType.New => "آکبند",
        InsuranceType.Used => "کارکرده",
        _ => type.ToString()
    };
}

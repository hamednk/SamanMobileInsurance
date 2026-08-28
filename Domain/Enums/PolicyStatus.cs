namespace SamanMobileInsurance.Domain.Enums;

public enum PolicyStatus
{
    Draft = 1,
    AwaitingImages = 2,
    AwaitingPayment = 3,
    Paid = 4,
    Issued = 5,
    Cancelled = 6,
    Expired = 7
}

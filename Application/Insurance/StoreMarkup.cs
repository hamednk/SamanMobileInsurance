using SamanMobileInsurance.Application.Common;

namespace SamanMobileInsurance.Application.Insurance;

public static class StoreMarkup
{
    public static decimal ResolveCustomerCharged(decimal premiumRial, decimal? requested)
    {
        var charged = requested ?? premiumRial;
        if (charged < premiumRial)
        {
            throw new ValidationAppException("مبلغ دریافتی از مشتری نمی‌تواند کمتر از حق بیمه (سهم شرکت) باشد.");
        }

        return charged;
    }

    public static decimal Profit(decimal customerChargedRial, decimal premiumRial) =>
        customerChargedRial - premiumRial;
}

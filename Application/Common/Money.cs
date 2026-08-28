namespace SamanMobileInsurance.Application.Common;

public static class Money
{
    public const int TomanToRial = 10;
    public const decimal BillionTomanRial = 10_000_000_000m;
    public const decimal HundredMillionTomanRial = 1_000_000_000m;

    public static decimal ToToman(decimal rial) => Math.Round(rial / TomanToRial, 0, MidpointRounding.AwayFromZero);

    public static decimal ToRial(decimal toman) => toman * TomanToRial;
}

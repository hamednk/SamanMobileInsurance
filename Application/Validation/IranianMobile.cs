using System.Text.RegularExpressions;

namespace SamanMobileInsurance.Application.Validation;

public static class IranianMobile
{
    private static readonly Regex Pattern = new(@"^09\d{9}$", RegexOptions.Compiled);

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value.Trim());
}

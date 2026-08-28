using System.Text.RegularExpressions;

namespace SamanMobileInsurance.Application.Validation;

public static class IranianPostalCode
{
    private static readonly Regex Pattern = new(@"^\d{10}$", RegexOptions.Compiled);

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && Pattern.IsMatch(value.Trim());
}

using System.Text.RegularExpressions;

namespace SamanMobileInsurance.Application.Validation;

public static class IranianPersonName
{
    private static readonly Regex InvalidChars = new(@"[\d_a-zA-Z]", RegexOptions.Compiled);

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !InvalidChars.IsMatch(value.Trim()) &&
        value.Trim().Length >= 2;
}

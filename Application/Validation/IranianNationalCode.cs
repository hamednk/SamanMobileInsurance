using System.Text.RegularExpressions;

namespace SamanMobileInsurance.Application.Validation;

public static class IranianNationalCode
{
    private static readonly Regex Digits = new(@"^\d{10}$", RegexOptions.Compiled);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Digits.IsMatch(value))
        {
            return false;
        }

        if (value.Distinct().Count() == 1)
        {
            return false;
        }

        var check = value[9] - '0';
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (value[i] - '0') * (10 - i);
        }

        var remainder = sum % 11;
        return remainder < 2 ? check == remainder : check == 11 - remainder;
    }
}

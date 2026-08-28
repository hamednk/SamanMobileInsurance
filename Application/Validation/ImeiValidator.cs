using System.Text.RegularExpressions;

namespace SamanMobileInsurance.Application.Validation;

public static class ImeiValidator
{
    private static readonly Regex Digits = new(@"^\d{15}$", RegexOptions.Compiled);

    public static bool IsValid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Digits.IsMatch(value))
        {
            return false;
        }

        return PassesLuhn(value);
    }

    public static bool PassesLuhn(string digits)
    {
        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9)
                {
                    n -= 9;
                }
            }

            sum += n;
            alternate = !alternate;
        }

        return sum % 10 == 0;
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;

namespace SamanMobileInsurance.Infrastructure.Auth;

public class CaptchaService : ICaptchaService
{
    private static readonly char[] Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache _cache;

    public CaptchaService(IMemoryCache cache) => _cache = cache;

    public CaptchaChallengeDto Create()
    {
        var id = Guid.NewGuid();
        var code = GenerateCode(5);
        _cache.Set(CacheKey(id), Normalize(code), Ttl);
        return new CaptchaChallengeDto(id, BuildSvg(code));
    }

    public void Validate(Guid captchaId, string? code)
    {
        if (captchaId == Guid.Empty || string.IsNullOrWhiteSpace(code))
        {
            throw new ValidationAppException("کد امنیتی الزامی است.");
        }

        var key = CacheKey(captchaId);
        if (!_cache.TryGetValue(key, out string? expected) || string.IsNullOrWhiteSpace(expected))
        {
            throw new ValidationAppException("کد امنیتی منقضی شده است. تصویر جدید بگیرید.");
        }

        _cache.Remove(key);

        if (!string.Equals(expected, Normalize(code), StringComparison.Ordinal))
        {
            throw new ValidationAppException("کد امنیتی نادرست است.");
        }
    }

    private static string CacheKey(Guid id) => $"captcha:{id:N}";

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string GenerateCode(int length)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(buffer);
    }

    private static string BuildSvg(string code)
    {
        var sb = new StringBuilder();
        sb.Append("""<svg xmlns="http://www.w3.org/2000/svg" width="180" height="56" viewBox="0 0 180 56" role="img" aria-label="captcha">""");
        sb.Append("""<rect width="180" height="56" rx="10" fill="#0F2744"/>""");

        for (var i = 0; i < 8; i++)
        {
            var x1 = RandomNumberGenerator.GetInt32(0, 180);
            var y1 = RandomNumberGenerator.GetInt32(0, 56);
            var x2 = RandomNumberGenerator.GetInt32(0, 180);
            var y2 = RandomNumberGenerator.GetInt32(0, 56);
            sb.Append($"<line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y2}\" stroke=\"#3B82F6\" stroke-opacity=\"0.35\" stroke-width=\"1\"/>");
        }

        for (var i = 0; i < code.Length; i++)
        {
            var x = 18 + i * 30 + RandomNumberGenerator.GetInt32(-2, 3);
            var y = 34 + RandomNumberGenerator.GetInt32(-4, 5);
            var rotate = RandomNumberGenerator.GetInt32(-18, 19);
            var size = RandomNumberGenerator.GetInt32(20, 26);
            sb.Append(
                $"<text x=\"{x}\" y=\"{y}\" fill=\"#F8FAFC\" font-size=\"{size}\" font-family=\"Segoe UI, Tahoma, sans-serif\" font-weight=\"700\" transform=\"rotate({rotate} {x} {y})\">{code[i]}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}

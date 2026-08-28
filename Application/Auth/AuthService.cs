using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SamanMobileInsurance.Application.Abstractions;
using SamanMobileInsurance.Application.Common;
using SamanMobileInsurance.Domain.Entities;

namespace SamanMobileInsurance.Application.Auth;

public class AuthService
{
    private const int MaxFailedLogins = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ITokenService _tokens;
    private readonly INotificationService _notifications;
    private readonly IAuditLogger _audit;

    public AuthService(
        IApplicationDbContext db,
        IPasswordHasherService passwordHasher,
        ITokenService tokens,
        INotificationService notifications,
        IAuditLogger audit)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _tokens = tokens;
        _notifications = notifications;
        _audit = audit;
    }

    public async Task<AuthTokensDto> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await _db.Users
            .Include(u => u.Store)
            .FirstOrDefaultAsync(u => u.Username == username && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAppException("نام کاربری یا رمز عبور نادرست است.");
        }

        if (user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow)
        {
            throw new ForbiddenAppException("حساب کاربری موقتاً قفل شده است. کمی بعد دوباره تلاش کنید.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenAppException("حساب کاربری غیرفعال است.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= MaxFailedLogins)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginCount = 0;
            }

            await _db.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAppException("نام کاربری یا رمز عبور نادرست است.");
        }

        if (user.Store is { IsActive: false })
        {
            throw new ForbiddenAppException("فروشگاه شما غیرفعال است.");
        }

        user.FailedLoginCount = 0;
        user.LockoutEnd = null;
        var tokens = await IssueTokensAsync(user, ip, cancellationToken);
        await _audit.LogAsync("login", nameof(User), user.Id.ToString(), cancellationToken);
        return tokens;
    }

    public async Task<AuthTokensDto> RefreshAsync(string refreshToken, string? ip, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException("توکن بازیابی نامعتبر است.");
        }

        var hash = _tokens.HashToken(refreshToken);
        var stored = await _db.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u.Store)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null || !stored.IsActive || stored.User.IsDeleted || !stored.User.IsActive)
        {
            throw new UnauthorizedAppException("توکن بازیابی نامعتبر است.");
        }

        stored.RevokedAt = DateTimeOffset.UtcNow;
        var next = await IssueTokensAsync(stored.User, ip, cancellationToken, stored);
        return next;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var hash = _tokens.HashToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (stored is not null && stored.RevokedAt is null)
        {
            stored.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.Include(u => u.Store).FirstOrDefaultAsync(
            u => u.Username == request.Username.Trim() && !u.IsDeleted && u.IsActive,
            cancellationToken);

        if (user is null)
        {
            throw new ValidationAppException("نام کاربری یافت نشد یا غیرفعال است.");
        }

        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashToken(raw),
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var destination = user.Store?.Mobile1 ?? user.Username;
        await _notifications.SendPasswordResetAsync(destination, raw, cancellationToken);
        await _audit.LogAsync("forgot-password", nameof(User), user.Id.ToString(), cancellationToken);
        return new ForgotPasswordResultDto(raw);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var hash = _tokens.HashToken(request.Token);
        var token = await _db.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null || token.UsedAt is not null || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new ValidationAppException("لینک بازیابی نامعتبر یا منقضی شده است.");
        }

        token.UsedAt = DateTimeOffset.UtcNow;
        token.User.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        token.User.FailedLoginCount = 0;
        token.User.LockoutEnd = null;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync("reset-password", nameof(User), token.UserId.ToString(), cancellationToken);
    }

    private async Task<AuthTokensDto> IssueTokensAsync(
        User user,
        string? ip,
        CancellationToken cancellationToken,
        RefreshToken? previous = null)
    {
        var storeId = user.Store?.Id;
        var access = _tokens.CreateAccessToken(user, storeId);
        var refresh = _tokens.CreateRefreshToken();
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokens.HashToken(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedByIp = ip,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        if (previous is not null)
        {
            previous.ReplacedByTokenId = entity.Id;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new AuthTokensDto(access, refresh, 15 * 60, user.Role.ToString(), user.Username, storeId);
    }
}

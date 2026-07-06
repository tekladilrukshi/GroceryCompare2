using GroceryCompare.Domain.Entities;
using GroceryCompare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GroceryCompare.Api.Auth;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

public interface IAuthService
{
    /// <returns>App tokens, or null when the Google ID token is invalid.</returns>
    Task<AuthTokens?> SignInWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);

    /// <summary>Exchanges an active refresh token for new tokens, revoking the old one.</summary>
    /// <returns>New tokens, or null when the refresh token is unknown, revoked, or expired.</returns>
    Task<AuthTokens?> RefreshAsync(string rawRefreshToken, CancellationToken cancellationToken = default);

    /// <summary>Revokes the given refresh token if it exists; idempotent.</summary>
    Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default);
}

public class AuthService(
    GroceryCompareDbContext db,
    IGoogleTokenValidator googleValidator,
    ITokenService tokenService,
    TimeProvider timeProvider) : IAuthService
{
    public async Task<AuthTokens?> SignInWithGoogleAsync(
        string idToken, CancellationToken cancellationToken = default)
    {
        var payload = await googleValidator.ValidateAsync(idToken, cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await db.Users.SingleOrDefaultAsync(
            u => u.GoogleSubjectId == payload.Subject, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                GoogleSubjectId = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.DisplayName,
                CreatedAt = now,
            };
            db.Users.Add(user);
        }
        else
        {
            // Keep profile fields current with what Google reports.
            user.Email = payload.Email;
            user.DisplayName = payload.DisplayName;
        }

        var refreshToken = tokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = refreshToken.TokenHash,
            CreatedAt = now,
            ExpiresAt = refreshToken.ExpiresAt,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            tokenService.CreateAccessToken(user),
            refreshToken.RawToken,
            refreshToken.ExpiresAt);
    }

    public async Task<AuthTokens?> RefreshAsync(
        string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = tokenService.HashRefreshToken(rawRefreshToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var existing = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is null || !existing.IsActive(now))
        {
            return null;
        }

        existing.RevokedAt = now;

        var replacement = tokenService.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = replacement.TokenHash,
            CreatedAt = now,
            ExpiresAt = replacement.ExpiresAt,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthTokens(
            tokenService.CreateAccessToken(existing.User!),
            replacement.RawToken,
            replacement.ExpiresAt);
    }

    public async Task LogoutAsync(string rawRefreshToken, CancellationToken cancellationToken = default)
    {
        var hash = tokenService.HashRefreshToken(rawRefreshToken);
        var existing = await db.RefreshTokens
            .SingleOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

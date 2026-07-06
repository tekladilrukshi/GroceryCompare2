using GroceryCompare.Domain.Entities;
using GroceryCompare.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GroceryCompare.Api.Auth;

public sealed record AuthTokens(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

public interface IAuthService
{
    /// <returns>App tokens, or null when the Google ID token is invalid.</returns>
    Task<AuthTokens?> SignInWithGoogleAsync(string idToken, CancellationToken cancellationToken = default);
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
}

namespace GroceryCompare.Api.Auth;

/// <summary>What the app needs from a validated Google ID token.</summary>
public sealed record GoogleTokenPayload(string Subject, string Email, string DisplayName);

public interface IGoogleTokenValidator
{
    /// <returns>The payload, or null when the token is invalid/expired.</returns>
    Task<GoogleTokenPayload?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

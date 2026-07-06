using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace GroceryCompare.Api.Auth;

public class GoogleTokenValidator(IOptions<GoogleAuthOptions> options) : IGoogleTokenValidator
{
    public async Task<GoogleTokenPayload?> ValidateAsync(
        string idToken, CancellationToken cancellationToken = default)
    {
        var clientId = options.Value.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "GoogleAuth:ClientId is not configured; Google sign-in cannot work.");
        }

        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
            return new GoogleTokenPayload(
                payload.Subject,
                payload.Email ?? "",
                payload.Name ?? payload.Email ?? "");
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}

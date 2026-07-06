using System.ComponentModel.DataAnnotations;
using GroceryCompare.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace GroceryCompare.Api.Controllers;

public sealed record GoogleLoginRequest([Required] string IdToken);

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> Google(
        GoogleLoginRequest request, CancellationToken cancellationToken)
    {
        var tokens = await authService.SignInWithGoogleAsync(request.IdToken, cancellationToken);
        if (tokens is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid Google token",
                Detail = "The Google ID token could not be validated.",
            });
        }

        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
    }
}

using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GroceryCompare.Api.Auth;
using GroceryCompare.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GroceryCompare.Api.Controllers;

public sealed record GoogleLoginRequest([Required] string IdToken);

public sealed record RefreshRequest([Required] string RefreshToken);

public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiresAt);

public sealed record MeResponse(int Id, string Email, string DisplayName);

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

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(
        RefreshRequest request, CancellationToken cancellationToken)
    {
        var tokens = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (tokens is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid refresh token",
                Detail = "The refresh token is unknown, revoked, or expired.",
            });
        }

        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.RefreshTokenExpiresAt);
    }

    // Deliberately always 204: revoking is idempotent and the response must not
    // leak whether a given token existed.
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me(
        [FromServices] GroceryCompareDbContext db, CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var user = await db.Users.FindAsync([userId], cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return new MeResponse(user.Id, user.Email, user.DisplayName);
    }
}

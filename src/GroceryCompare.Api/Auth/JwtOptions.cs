using System.ComponentModel.DataAnnotations;

namespace GroceryCompare.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; set; } = "";

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; set; } = "";

    /// <summary>HMAC signing key, at least 32 characters. Dev value lives in
    /// appsettings.Development.json; production comes from Key Vault.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string SigningKey { get; set; } = "";

    [Range(1, 24 * 60)]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    [Range(1, 365)]
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}

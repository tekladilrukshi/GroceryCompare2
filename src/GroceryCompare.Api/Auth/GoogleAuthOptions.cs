namespace GroceryCompare.Api.Auth;

public class GoogleAuthOptions
{
    public const string SectionName = "GoogleAuth";

    /// <summary>OAuth 2.0 client ID; must match the SPA's VITE_GOOGLE_CLIENT_ID
    /// so ID-token audience validation passes.</summary>
    public string ClientId { get; set; } = "";
}

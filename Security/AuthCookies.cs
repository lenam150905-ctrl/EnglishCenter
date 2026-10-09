namespace EnglishCenter.API.Security;

public static class AuthCookies
{
    // No Domain attribute is set, so these remain host-only cookies.
    // Production additionally requires HTTPS via CookieOptions.Secure.
    public const string AccessToken = "ec_access";
    public const string RefreshToken = "ec_refresh";
}

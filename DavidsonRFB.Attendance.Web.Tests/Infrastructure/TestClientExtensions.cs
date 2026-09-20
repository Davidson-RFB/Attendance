using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DavidsonRFB.Attendance.Web.Tests.Infrastructure;

internal static partial class TestClientExtensions
{
    /// <summary>
    /// Creates an HttpClient and signs it in via the FakeAadSignIn test-only
    /// endpoint (see that class for why this is a legitimate way to test the
    /// real "Cookies" scheme mechanics without an interactive AAD login).
    /// </summary>
    public static async Task<HttpClient> CreateAadAuthenticatedClientAsync(
        this CustomWebApplicationFactory factory,
        string email = CustomWebApplicationFactory.TestEmail,
        string name = CustomWebApplicationFactory.TestEmployeeName,
        bool allowAutoRedirect = true)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = allowAutoRedirect });
        var signIn = await client.GetAsync($"/__test/signin?email={Uri.EscapeDataString(email)}&name={Uri.EscapeDataString(name)}");
        signIn.EnsureSuccessStatusCode();
        return client;
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiForgeryTokenRegex();

    public static async Task<string> ExtractAntiForgeryTokenAsync(this HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var match = AntiForgeryTokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"No __RequestVerificationToken hidden field found in response body (status {response.StatusCode}):\n{html}");
        }

        return match.Groups[1].Value;
    }

    /// <summary>
    /// Pulls a cookie's raw value out of a response's Set-Cookie headers
    /// (used to carry the Kiosk cookie into a deliberately separate,
    /// AAD-session-free HttpClient - simulating a kiosk browser that only
    /// ever has the Kiosk cookie, never an AAD session).
    /// </summary>
    public static string ExtractSetCookieValue(this HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            throw new InvalidOperationException($"Response had no Set-Cookie headers at all (status {response.StatusCode}).");
        }

        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(cookieName + "=", StringComparison.Ordinal))
            {
                var afterName = cookie[(cookieName.Length + 1)..];
                var semicolon = afterName.IndexOf(';');
                return semicolon >= 0 ? afterName[..semicolon] : afterName;
            }
        }

        throw new InvalidOperationException(
            $"No Set-Cookie header for '{cookieName}' found. Headers were: {string.Join(" | ", cookies)}");
    }

    /// <summary>
    /// True if any Set-Cookie header for the given cookie name looks like an
    /// expiry/removal (empty value and/or an expires date in the past) -
    /// this is how HttpContext.SignOutAsync revokes a cookie: it doesn't
    /// track server-side revocation, it just tells the client to delete it.
    /// </summary>
    public static bool HasExpiredSetCookie(this HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return false;
        }

        foreach (var cookie in cookies)
        {
            if (cookie.StartsWith(cookieName + "=", StringComparison.Ordinal) &&
                cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase))
            {
                var expiresIndex = cookie.IndexOf("expires=", StringComparison.OrdinalIgnoreCase) + "expires=".Length;
                var rest = cookie[expiresIndex..];
                var semicolon = rest.IndexOf(';');
                var expiresValue = semicolon >= 0 ? rest[..semicolon] : rest;
                if (DateTimeOffset.TryParse(expiresValue, out var expires) && expires < DateTimeOffset.UtcNow)
                {
                    return true;
                }
            }
        }

        return false;
    }
}

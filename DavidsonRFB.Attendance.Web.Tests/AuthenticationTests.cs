using System.Net;
using DavidsonRFB.Attendance.Web.Auth;
using DavidsonRFB.Attendance.Web.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DavidsonRFB.Attendance.Web.Tests;

/// <summary>
/// End-to-end verification of the auth design intent in
/// docs/DOTNET10_MIGRATION_PLAN.md section 4:
///   1. Home/Index (and ClockOut, by the same attribute) accept EITHER a
///      real AAD session OR the Kiosk cookie.
///   2. CreateCookie/RemoveCookie strictly require AAD - never the Kiosk
///      cookie alone.
///   3. Every other page (Employee/Attendance) is AAD-only; the Kiosk cookie
///      must never grant access there.
///
/// Also covers the real bug found and fixed while building this suite: bare
/// [Authorize] never actually authenticated the "Cookies" scheme under this
/// app's `AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)`
/// registration - see KioskAuthentication.AadOnlyScheme's doc comment and
/// the Decisions log in docs/DOTNET10_MIGRATION_PLAN.md.
/// </summary>
[Collection(IntegrationCollection.Name)]
public class AuthenticationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Employee")]
    [InlineData("/Attendance")]
    [InlineData("/Home/CreateCookie")]
    public async Task Unauthenticated_ProtectedPages_RedirectToRealAad(string path)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("login.microsoftonline.com", location);
        Assert.Contains("oauth2/v2.0/authorize", location);
    }

    [Fact]
    public async Task AadAuthenticated_CanAccessHomeEmployeeAndAttendance()
    {
        var client = await _factory.CreateAadAuthenticatedClientAsync();

        var homeResponse = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

        var employeeResponse = await client.GetAsync("/Employee");
        Assert.Equal(HttpStatusCode.OK, employeeResponse.StatusCode);

        var attendanceResponse = await client.GetAsync("/Attendance");
        Assert.Equal(HttpStatusCode.OK, attendanceResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCookie_MintsKioskCookie_ThatWorksOnHomeOnly_AndCannotSelfProvisionOrReachAadOnlyPages()
    {
        // Arrange: a real AAD-authenticated session mints the Kiosk cookie.
        var aadClient = await _factory.CreateAadAuthenticatedClientAsync(allowAutoRedirect: false);
        var createCookieResponse = await aadClient.GetAsync("/Home/CreateCookie");
        Assert.Equal(HttpStatusCode.Redirect, createCookieResponse.StatusCode); // RedirectToAction(Index)

        var kioskCookieValue = createCookieResponse.ExtractSetCookieValue(KioskAuthentication.CookieName);
        Assert.False(string.IsNullOrWhiteSpace(kioskCookieValue));

        // Act: a totally separate client, carrying ONLY the Kiosk cookie -
        // no AAD session at all - simulating a kiosk terminal's browser.
        var kioskOnlyClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false,
        });
        kioskOnlyClient.DefaultRequestHeaders.Add("Cookie", $"{KioskAuthentication.CookieName}={kioskCookieValue}");

        // Assert: Home/Index succeeds via the Kiosk cookie alone (OR semantics).
        var homeResponse = await kioskOnlyClient.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, homeResponse.StatusCode);

        // Assert: every AAD-only surface rejects the Kiosk-only client and
        // challenges to AAD instead - the Kiosk cookie must never grant
        // access beyond Home/ClockOut, INCLUDING re-provisioning itself.
        foreach (var path in new[] { "/Employee", "/Attendance", "/Home/CreateCookie", "/Home/RemoveCookie" })
        {
            var response = await kioskOnlyClient.GetAsync(path);
            Assert.True(
                response.StatusCode == HttpStatusCode.Redirect,
                $"Expected {path} to redirect (challenge) for a Kiosk-only session, got {response.StatusCode}.");
            var location = response.Headers.Location?.ToString() ?? string.Empty;
            Assert.Contains("login.microsoftonline.com", location);
        }
    }

    [Fact]
    public async Task RemoveCookie_ExpiresTheKioskCookie()
    {
        var aadClient = await _factory.CreateAadAuthenticatedClientAsync(allowAutoRedirect: false);
        var createCookieResponse = await aadClient.GetAsync("/Home/CreateCookie");
        Assert.Equal(HttpStatusCode.Redirect, createCookieResponse.StatusCode);

        var removeCookieResponse = await aadClient.GetAsync("/Home/RemoveCookie");

        Assert.Equal(HttpStatusCode.Redirect, removeCookieResponse.StatusCode);
        Assert.True(
            removeCookieResponse.HasExpiredSetCookie(KioskAuthentication.CookieName),
            "Expected RemoveCookie's response to include an expired Set-Cookie for the Kiosk cookie.");
    }
}

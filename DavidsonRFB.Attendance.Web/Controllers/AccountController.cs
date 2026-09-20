using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;

namespace DavidsonRFB.Attendance.Web.Controllers;

/// <summary>
/// Ported from the legacy AccountController's OWIN-based SignIn/SignOut into
/// ASP.NET Core's Authentication service. One small, deliberate behavior
/// improvement over the original: SignIn now redirects Home if already
/// authenticated instead of silently rendering a blank response.
/// </summary>
public class AccountController : Controller
{
    [HttpGet]
    public IActionResult SignIn()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return Challenge(
            new AuthenticationProperties { RedirectUri = Url.Action("Index", "Home") },
            OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Deliberately shadows ControllerBase.SignOut() - this is the action
    // method for the /Account/SignOut route, not the SignOut() helper.
    [HttpGet]
    public new IActionResult SignOut()
    {
        var callbackUrl = Url.Action(nameof(SignOutCallback), "Account", values: null, protocol: Request.Scheme);

        return SignOut(
            new AuthenticationProperties { RedirectUri = callbackUrl },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult SignOutCallback()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }
}

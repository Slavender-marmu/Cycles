using System.Text;
using System.Text.Encodings.Web;
using BicycleRacing.Api.Models;
using BicycleRacing.Database.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace BicycleRacing.Api.Controllers;

[AllowAnonymous]
[AutoValidateAntiforgeryToken]
[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
public class AccountController(
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    IEmailSender emailSender) : Controller
{
    [HttpGet("enter-account")]
    public IActionResult EnterAccount(string? returnUrl = null)
    {
        var destination = LocalDestination(returnUrl);
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(destination);
        }

        return View(new EnterAccountViewModel { ReturnUrl = destination });
    }

    [HttpPost("enter-account")]
    public async Task<IActionResult> EnterAccount(string form, string? returnUrl = null)
    {
        var model = new EnterAccountViewModel { ReturnUrl = LocalDestination(returnUrl) };
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(model.ReturnUrl);
        }

        // Bind and validate only the submitted form; the other form can remain empty.
        switch (form)
        {
            case "signup":
                if (await TryUpdateModelAsync(model.Signup, nameof(model.Signup)))
                {
                    return await SignupAsync(model);
                }
                break;
            case "login":
                if (await TryUpdateModelAsync(model.Login, nameof(model.Login)))
                {
                    return await LoginAsync(model);
                }
                break;
            default:
                return BadRequest();
        }

        return View(model);
    }

    private async Task<IActionResult> SignupAsync(EnterAccountViewModel model)
    {
        var user = new User { UserName = model.Signup.Email, Email = model.Signup.Email };
        var result = await userManager.CreateAsync(user, model.Signup.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(nameof(model.Signup), error.Description);
            }
            return View(nameof(EnterAccount), model);
        }

        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(
            await userManager.GenerateEmailConfirmationTokenAsync(user)));
        var callbackUrl = Url.Page("/Account/ConfirmEmail", pageHandler: null,
            values: new { area = "Identity", userId = user.Id, code, returnUrl = model.ReturnUrl },
            protocol: Request.Scheme)!;
        await emailSender.SendEmailAsync(model.Signup.Email, "Confirm your email",
            $"Confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

        if (userManager.Options.SignIn.RequireConfirmedAccount)
        {
            return RedirectToPage("/Account/RegisterConfirmation",
                new { area = "Identity", email = model.Signup.Email, returnUrl = model.ReturnUrl });
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(model.ReturnUrl!);
    }

    private async Task<IActionResult> LoginAsync(EnterAccountViewModel model)
    {
        var result = await signInManager.PasswordSignInAsync(model.Login.Email, model.Login.Password,
            model.Login.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return LocalRedirect(model.ReturnUrl!);
        }

        if (result.RequiresTwoFactor)
        {
            return RedirectToPage("/Account/LoginWith2fa",
                new { area = "Identity", returnUrl = model.ReturnUrl, rememberMe = model.Login.RememberMe });
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError(nameof(model.Login),
                "This account is temporarily locked. Please try again later.");
        }
        else
        {
            ModelState.AddModelError(nameof(model.Login),
                "Unable to log in. Check your email and password, and make sure your email is confirmed.");
        }

        return View(nameof(EnterAccount), model);
    }

    private string LocalDestination(string? returnUrl) =>
        Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Content("~/");
}

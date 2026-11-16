using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BicycleRacing.Api.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class EnterAccountRedirectModel : PageModel
{
    public IActionResult OnGet(string? returnUrl = null) =>
        RedirectToAction("EnterAccount", "Account", new { area = "", returnUrl });
}

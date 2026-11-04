using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vulindlela.Portal.Mvc.Models;
using Vulindlela.Portal.Mvc.Security;
using Vulindlela.Portal.Mvc.Services;

namespace Vulindlela.Portal.Mvc.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult SignIn(string? returnUrl)
        {
            return View(new SignInViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountService.SignInAsync(model.Username, model.Password);
            if (!result.Succeeded)
            {
                model.Error = "The ID number or password is incorrect.";
                model.Password = "";
                return View(model);
            }

            await SignInCookieAsync(result.Account!);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var outcome = await _accountService.RegisterCitizenAsync(model.CitizenId, model.DisplayName, model.Password);
            if (outcome != RegisterOutcome.Registered)
            {
                model.Error = outcome == RegisterOutcome.InvalidCitizenId
                    ? "That is not a valid South African ID number."
                    : "An account already exists for that ID number. Please sign in.";
                model.Password = "";
                model.ConfirmPassword = "";
                return View(model);
            }

            var signedIn = await _accountService.SignInAsync(model.CitizenId, model.Password);
            await SignInCookieAsync(signedIn.Account!);
            return RedirectToAction("Index", "Booking");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public new async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private Task SignInCookieAsync(Account account)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new(ClaimTypes.Name, account.DisplayName),
                new(ClaimTypes.Role, account.Role)
            };
            if (account.CitizenId is not null)
            {
                claims.Add(new(PortalClaims.CitizenId, account.CitizenId));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        }
    }
}

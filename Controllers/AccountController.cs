using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data.Repositories;
using VertexLearn.Infrastructure;
using VertexLearn.Models;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _users;
        private readonly IPasswordHasher<User> _hasher;

        public AccountController(UserRepository users, IPasswordHasher<User> hasher)
        {
            _users = users;
            _hasher = hasher;
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToHome();
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register — creates a Student account (INSERT into Users)
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid && await _users.EmailExistsAsync(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
            }
            if (!ModelState.IsValid) return View(model);

            var user = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Role = Roles.Student
            };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);
            user.UserId = await _users.CreateAsync(user);

            await AuthHelper.SignInAsync(HttpContext, user, persistent: false);
            TempData["Success"] = $"Welcome to VertexLearn, {user.FullName}! Browse the catalogue to enrol in your first course.";
            return RedirectToAction("Dashboard", "Member");
        }

        // GET: /Account/Login
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return RedirectToHome();
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        // POST: /Account/Login — verifies the hashed password and issues the auth cookie
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _users.GetByEmailAsync(model.Email);
            var result = user == null
                ? PasswordVerificationResult.Failed
                : _hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (user == null || result == PasswordVerificationResult.Failed)
            {
                // Same message for both cases so attackers can't discover which emails exist
                ModelState.AddModelError(string.Empty, "The email or password is incorrect.");
                return View(model);
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                await _users.UpdatePasswordAsync(user.UserId, _hasher.HashPassword(user, model.Password));
            }

            await AuthHelper.SignInAsync(HttpContext, user, model.RememberMe);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return user.Role == Roles.Admin
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Dashboard", "Member");
        }

        // POST: /Account/Logout
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "You have been logged out.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied() => View();

        private IActionResult RedirectToHome() =>
            User.IsAdmin() ? RedirectToAction("Index", "Admin") : RedirectToAction("Dashboard", "Member");
    }
}

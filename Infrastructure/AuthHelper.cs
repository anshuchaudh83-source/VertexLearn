using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using VertexLearn.Models;

namespace VertexLearn.Infrastructure
{
    public static class AuthHelper
    {
        /// <summary>Issues the authentication cookie holding the user's id, name, email and role.</summary>
        public static Task SignInAsync(HttpContext context, User user, bool persistent)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = persistent,
                    ExpiresUtc = persistent ? DateTimeOffset.UtcNow.AddDays(14) : null
                });
        }
    }
}

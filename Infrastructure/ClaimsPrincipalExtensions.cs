using System.Security.Claims;
using VertexLearn.Models;

namespace VertexLearn.Infrastructure
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>The logged-in user's UserId (0 for anonymous visitors).</summary>
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);

        public static bool IsStudent(this ClaimsPrincipal user) => user.IsInRole(Roles.Student);

        public static string FirstName(this ClaimsPrincipal user) =>
            (user.Identity?.Name ?? "there").Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there";
    }
}

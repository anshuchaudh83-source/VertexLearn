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
    /// <summary>Registered-member module. Every action requires a valid login.</summary>
    [Authorize]
    public class MemberController : Controller
    {
        private readonly UserRepository _users;
        private readonly CourseRepository _courses;
        private readonly ResourceRepository _resources;
        private readonly EnrollmentRepository _enrollments;
        private readonly QuizRepository _quizzes;
        private readonly DiscussionRepository _discussions;
        private readonly IPasswordHasher<User> _hasher;

        public MemberController(UserRepository users, CourseRepository courses, ResourceRepository resources,
                                EnrollmentRepository enrollments, QuizRepository quizzes,
                                DiscussionRepository discussions, IPasswordHasher<User> hasher)
        {
            _users = users;
            _courses = courses;
            _resources = resources;
            _enrollments = enrollments;
            _quizzes = quizzes;
            _discussions = discussions;
            _hasher = hasher;
        }

        // GET: /Member/Dashboard
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.GetUserId();
            var attempts = await _quizzes.GetAttemptsForUserAsync(userId);

            var model = new StudentDashboardViewModel
            {
                FullName = User.Identity?.Name ?? "",
                Courses = await _enrollments.GetForUserAsync(userId),
                RecentAttempts = attempts.Take(5).ToList(),
                TotalAttempts = attempts.Count,
                AveragePercent = attempts.Count == 0 ? 0 : await _quizzes.GetAveragePercentAsync(userId),
                PostCount = await _discussions.CountByUserAsync(userId)
            };
            return View(model);
        }

        // POST: /Member/Enroll/5  (INSERT into Enrollments)
        [HttpPost]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Enroll(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _enrollments.EnrollAsync(User.GetUserId(), id);
            TempData["Success"] = $"You're enrolled in {course.Title}.";
            return RedirectToAction(nameof(Learn), new { id });
        }

        // POST: /Member/Unenroll/5  (DELETE from Enrollments)
        [HttpPost]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Unenroll(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();

            await _enrollments.UnenrollAsync(User.GetUserId(), id);
            TempData["Success"] = $"You have left {course.Title}. Your quiz history is kept.";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: /Member/Learn/5 — course materials, quizzes and discussion (enrolled students & admins)
        public async Task<IActionResult> Learn(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();

            if (!User.IsAdmin() && !await _enrollments.IsEnrolledAsync(User.GetUserId(), id))
            {
                TempData["Error"] = "Enrol in this course to open its materials.";
                return RedirectToAction("Details", "Courses", new { id });
            }

            var model = new LearnViewModel
            {
                Course = course,
                Resources = await _resources.GetByCourseAsync(id),
                Quizzes = await _quizzes.GetForCourseAsync(id, User.GetUserId()),
                RecentPosts = await _discussions.GetByCourseAsync(id, top: 3)
            };
            return View(model);
        }

        // GET: /Member/Results — full quiz history
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> Results()
        {
            var attempts = await _quizzes.GetAttemptsForUserAsync(User.GetUserId());
            return View(attempts);
        }

        // GET: /Member/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _users.GetByIdAsync(User.GetUserId());
            if (user == null) return await ForceLogout();

            return View(new ProfilePageViewModel
            {
                Profile = new ProfileViewModel
                {
                    FullName = user.FullName,
                    Email = user.Email,
                    Bio = user.Bio,
                    MemberSince = user.CreatedAt
                }
            });
        }

        // POST: /Member/Profile  (UPDATE Users)
        [HttpPost]
        public async Task<IActionResult> Profile([Bind(Prefix = "Profile")] ProfileViewModel model)
        {
            var user = await _users.GetByIdAsync(User.GetUserId());
            if (user == null) return await ForceLogout();

            if (ModelState.IsValid && await _users.EmailExistsAsync(model.Email, user.UserId))
            {
                ModelState.AddModelError("Profile.Email", "Another account already uses this email.");
            }
            if (!ModelState.IsValid)
            {
                model.MemberSince = user.CreatedAt;
                return View(new ProfilePageViewModel { Profile = model });
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();
            await _users.UpdateAsync(user);

            // Refresh the cookie so the new name shows in the navigation straight away
            await AuthHelper.SignInAsync(HttpContext, user, persistent: false);
            TempData["Success"] = "Profile saved.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Member/ChangePassword  (UPDATE Users.PasswordHash)
        [HttpPost]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "Password")] ChangePasswordViewModel model)
        {
            var user = await _users.GetByIdAsync(User.GetUserId());
            if (user == null) return await ForceLogout();

            if (ModelState.IsValid &&
                _hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password.CurrentPassword", "Your current password is incorrect.");
            }
            if (!ModelState.IsValid)
            {
                var page = new ProfilePageViewModel
                {
                    Profile = new ProfileViewModel { FullName = user.FullName, Email = user.Email, Bio = user.Bio, MemberSince = user.CreatedAt },
                    Password = new ChangePasswordViewModel()
                };
                ViewData["OpenPasswordPanel"] = true;
                return View(nameof(Profile), page);
            }

            await _users.UpdatePasswordAsync(user.UserId, _hasher.HashPassword(user, model.NewPassword));
            TempData["Success"] = "Password changed.";
            return RedirectToAction(nameof(Profile));
        }

        // POST: /Member/DeleteAccount  (DELETE from Users — students only)
        [HttpPost]
        [Authorize(Roles = Roles.Student)]
        public async Task<IActionResult> DeleteAccount(string confirmEmail)
        {
            var user = await _users.GetByIdAsync(User.GetUserId());
            if (user == null) return await ForceLogout();

            if (!string.Equals(confirmEmail?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "The email you typed doesn't match your account, so nothing was deleted.";
                return RedirectToAction(nameof(Profile));
            }

            await _users.DeleteAsync(user.UserId);
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Success"] = "Your account and all of its data have been deleted.";
            return RedirectToAction("Index", "Home");
        }

        private async Task<IActionResult> ForceLogout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}

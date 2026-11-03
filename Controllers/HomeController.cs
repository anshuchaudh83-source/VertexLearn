using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data.Repositories;
using VertexLearn.Models;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    public class HomeController : Controller
    {
        private readonly CourseRepository _courses;
        private readonly CategoryRepository _categories;
        private readonly UserRepository _users;
        private readonly QuizRepository _quizzes;
        private readonly ContactRepository _contact;

        public HomeController(CourseRepository courses, CategoryRepository categories, UserRepository users,
                              QuizRepository quizzes, ContactRepository contact)
        {
            _courses = courses;
            _categories = categories;
            _users = users;
            _quizzes = quizzes;
            _contact = contact;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            var model = new HomeViewModel
            {
                FeaturedCourses = await _courses.GetFeaturedAsync(3),
                Categories = await _categories.GetAllAsync(),
                CourseCount = await _courses.CountAsync(),
                StudentCount = await _users.CountByRoleAsync(Roles.Student),
                QuizCount = await _quizzes.CountAsync()
            };
            return View(model);
        }

        // GET: /Home/About
        public IActionResult About() => View();

        // GET: /Home/Contact
        public IActionResult Contact() => View(new ContactMessage
        {
            Name = User.Identity?.IsAuthenticated == true ? User.Identity.Name ?? "" : "",
            Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? ""
        });

        // POST: /Home/Contact  (INSERT into ContactMessages)
        [HttpPost]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (!ModelState.IsValid) return View(model);

            await _contact.CreateAsync(model);
            TempData["Success"] = "Your message has been sent. We'll reply by email.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Error() => View();

        // Friendly pages for 404 / 403 etc.
        [Route("Home/StatusCode")]
        public IActionResult StatusCodePage(int code)
        {
            ViewData["Code"] = code;
            return View("StatusCode");
        }
    }
}

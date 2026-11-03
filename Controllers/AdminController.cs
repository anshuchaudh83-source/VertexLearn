using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VertexLearn.Data.Repositories;
using VertexLearn.Infrastructure;
using VertexLearn.Models;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    /// <summary>Administrator module: manages all website content and the database. Admin role only.</summary>
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : Controller
    {
        private readonly UserRepository _users;
        private readonly CategoryRepository _categories;
        private readonly CourseRepository _courses;
        private readonly ResourceRepository _resources;
        private readonly QuizRepository _quizzes;
        private readonly EnrollmentRepository _enrollments;
        private readonly DiscussionRepository _discussions;
        private readonly ContactRepository _contact;
        private readonly IPasswordHasher<User> _hasher;
        private readonly IWebHostEnvironment _env;

        // Allowed upload extensions per resource type, and the size limit for uploads
        private const long MaxUploadBytes = 20 * 1024 * 1024; // 20 MB
        private static readonly Dictionary<string, string[]> UploadExtensions = new()
        {
            ["PDF"] = new[] { ".pdf" },
            ["Notes"] = new[] { ".pdf", ".txt", ".md", ".docx", ".pptx" },
            ["Image"] = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" },
            ["Audio"] = new[] { ".mp3", ".wav", ".ogg" },
            ["Video"] = new[] { ".mp4", ".webm" }
        };

        public AdminController(UserRepository users, CategoryRepository categories, CourseRepository courses,
                               ResourceRepository resources, QuizRepository quizzes, EnrollmentRepository enrollments,
                               DiscussionRepository discussions, ContactRepository contact, IPasswordHasher<User> hasher,
                               IWebHostEnvironment env)
        {
            _env = env;
            _users = users;
            _categories = categories;
            _courses = courses;
            _resources = resources;
            _quizzes = quizzes;
            _enrollments = enrollments;
            _discussions = discussions;
            _contact = contact;
            _hasher = hasher;
        }

        /* ======================== Dashboard ======================== */

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                StudentCount = await _users.CountByRoleAsync(Roles.Student),
                AdminCount = await _users.CountByRoleAsync(Roles.Admin),
                CourseCount = await _courses.CountAsync(),
                ResourceCount = await _resources.CountAsync(),
                QuizCount = await _quizzes.CountAsync(),
                PostCount = await _discussions.CountAsync(),
                UnreadMessages = await _contact.CountUnreadAsync(),
                RecentUsers = await _users.GetRecentAsync(5),
                RecentEnrollments = await _enrollments.GetRecentAsync(5),
                RecentAttempts = await _quizzes.GetRecentAttemptsAsync(5)
            };
            return View(model);
        }

        /* ======================== Users ======================== */

        public async Task<IActionResult> Users(string? search, string? role)
        {
            ViewData["Search"] = search;
            ViewData["Role"] = role;
            return View(await _users.GetAllAsync(search, role));
        }

        public IActionResult CreateUser() => View("UserForm", new UserFormViewModel());

        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _users.GetByIdAsync(id);
            if (user == null) return NotFound();
            return View("UserForm", new UserFormViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                Bio = user.Bio
            });
        }

        [HttpPost]
        public async Task<IActionResult> SaveUser(UserFormViewModel model)
        {
            if (model.IsNew && string.IsNullOrWhiteSpace(model.Password))
                ModelState.AddModelError(nameof(model.Password), "Set a password for the new account.");
            if (ModelState.IsValid && await _users.EmailExistsAsync(model.Email, model.UserId))
                ModelState.AddModelError(nameof(model.Email), "Another account already uses this email.");
            // An admin can't demote themselves and lock themselves out
            if (!model.IsNew && model.UserId == User.GetUserId() && model.Role != Roles.Admin)
                ModelState.AddModelError(nameof(model.Role), "You can't remove your own admin role.");

            if (!ModelState.IsValid) return View("UserForm", model);

            if (model.IsNew)
            {
                var user = new User { FullName = model.FullName, Email = model.Email, Role = model.Role, Bio = model.Bio };
                user.PasswordHash = _hasher.HashPassword(user, model.Password!);
                await _users.CreateAsync(user);
                TempData["Success"] = $"Account created for {user.FullName}.";
            }
            else
            {
                var user = await _users.GetByIdAsync(model.UserId);
                if (user == null) return NotFound();
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Role = model.Role;
                user.Bio = model.Bio;
                await _users.UpdateAsync(user);
                if (!string.IsNullOrWhiteSpace(model.Password))
                    await _users.UpdatePasswordAsync(user.UserId, _hasher.HashPassword(user, model.Password));
                TempData["Success"] = $"{user.FullName}'s account was updated.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id == User.GetUserId())
            {
                TempData["Error"] = "You can't delete the account you're logged in with.";
                return RedirectToAction(nameof(Users));
            }
            var user = await _users.GetByIdAsync(id);
            if (user == null) return NotFound();

            await _users.DeleteAsync(id);
            TempData["Success"] = $"{user.FullName}'s account and related data were deleted.";
            return RedirectToAction(nameof(Users));
        }

        /* ======================== Categories ======================== */

        public async Task<IActionResult> Categories() => View(await _categories.GetAllAsync());

        public IActionResult CreateCategory() => View("CategoryForm", new Category());

        public async Task<IActionResult> EditCategory(int id)
        {
            var category = await _categories.GetByIdAsync(id);
            return category == null ? NotFound() : View("CategoryForm", category);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCategory(Category model)
        {
            if (ModelState.IsValid && await _categories.NameExistsAsync(model.Name, model.CategoryId))
                ModelState.AddModelError(nameof(model.Name), "A category with this name already exists.");
            if (!ModelState.IsValid) return View("CategoryForm", model);

            if (model.CategoryId == 0) await _categories.CreateAsync(model);
            else await _categories.UpdateAsync(model);

            TempData["Success"] = $"Category \"{model.Name}\" saved.";
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _categories.GetByIdAsync(id);
            if (category == null) return NotFound();
            if (category.CourseCount > 0)
            {
                TempData["Error"] = $"\"{category.Name}\" still has {category.CourseCount} course(s). Move or delete them first.";
                return RedirectToAction(nameof(Categories));
            }
            await _categories.DeleteAsync(id);
            TempData["Success"] = $"Category \"{category.Name}\" deleted.";
            return RedirectToAction(nameof(Categories));
        }

        /* ======================== Courses ======================== */

        public async Task<IActionResult> Courses(int? categoryId, string? search)
        {
            ViewData["Search"] = search;
            ViewData["CategoryId"] = categoryId;
            ViewBag.Categories = await _categories.GetAllAsync();
            return View(await _courses.GetAllAsync(categoryId, search));
        }

        public async Task<IActionResult> CreateCourse()
        {
            await LoadCategoryOptions();
            return View("CourseForm", new Course());
        }

        public async Task<IActionResult> EditCourse(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();
            await LoadCategoryOptions();
            return View("CourseForm", course);
        }

        [HttpPost]
        public async Task<IActionResult> SaveCourse(Course model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoryOptions();
                return View("CourseForm", model);
            }

            if (model.CourseId == 0) await _courses.CreateAsync(model);
            else await _courses.UpdateAsync(model);

            TempData["Success"] = $"Course \"{model.Title}\" saved.";
            return RedirectToAction(nameof(Courses));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();
            await _courses.DeleteAsync(id);
            TempData["Success"] = $"\"{course.Title}\" and its resources, quizzes and discussions were deleted.";
            return RedirectToAction(nameof(Courses));
        }

        /* ======================== Resources ======================== */

        public async Task<IActionResult> Resources(int? courseId)
        {
            ViewData["CourseId"] = courseId;
            ViewBag.Courses = await _courses.GetAllAsync();
            return View(await _resources.GetAllAsync(courseId));
        }

        public async Task<IActionResult> CreateResource(int? courseId)
        {
            await LoadCourseOptions();
            return View("ResourceForm", new Resource { CourseId = courseId ?? 0 });
        }

        public async Task<IActionResult> EditResource(int id)
        {
            var resource = await _resources.GetByIdAsync(id);
            if (resource == null) return NotFound();
            await LoadCourseOptions();
            return View("ResourceForm", resource);
        }

        [HttpPost]
        [RequestSizeLimit(MaxUploadBytes + 1024 * 1024)]
        public async Task<IActionResult> SaveResource(Resource model)
        {
            var file = model.UploadFile;
            var hasFile = file != null && file.Length > 0;

            // 1. Either a URL/path or an uploaded file is required
            if (!hasFile && string.IsNullOrWhiteSpace(model.Url))
            {
                ModelState.AddModelError(nameof(model.Url), "Enter a URL or file path, or upload a file.");
            }

            // 2. Validate the uploaded file (type and size)
            string? extension = null;
            if (hasFile)
            {
                extension = Path.GetExtension(file!.FileName).ToLowerInvariant();
                if (!UploadExtensions.TryGetValue(model.Type, out var allowed))
                {
                    ModelState.AddModelError(nameof(model.UploadFile), "Links can't be uploaded. Paste the web address instead.");
                }
                else if (!allowed.Contains(extension))
                {
                    ModelState.AddModelError(nameof(model.UploadFile), $"A {model.Type} resource must be one of: {string.Join(", ", allowed)}.");
                }
                else if (file.Length > MaxUploadBytes)
                {
                    ModelState.AddModelError(nameof(model.UploadFile), "The file is larger than 20 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                await LoadCourseOptions();
                return View("ResourceForm", model);
            }

            // 3. Save the file under wwwroot/files/uploads with a random name (never trust the user's file name)
            string? previousUrl = null;
            if (hasFile)
            {
                var folder = Path.Combine(_env.WebRootPath, "files", "uploads");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                {
                    await file!.CopyToAsync(stream);
                }

                if (model.ResourceId != 0)
                {
                    previousUrl = (await _resources.GetByIdAsync(model.ResourceId))?.Url;
                }
                model.Url = $"/files/uploads/{fileName}";
            }

            if (model.ResourceId == 0) await _resources.CreateAsync(model);
            else await _resources.UpdateAsync(model);

            // A replaced upload is no longer used by anything, so remove it from disk
            DeleteUploadedFile(previousUrl);

            TempData["Success"] = $"Resource \"{model.Title}\" saved.";
            return RedirectToAction(nameof(Resources), new { courseId = model.CourseId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _resources.GetByIdAsync(id);
            if (resource == null) return NotFound();
            await _resources.DeleteAsync(id);
            DeleteUploadedFile(resource.Url);
            TempData["Success"] = $"Resource \"{resource.Title}\" deleted.";
            return RedirectToAction(nameof(Resources));
        }

        /// <summary>Deletes a file that was uploaded through the admin form. Seeded files and external links are left alone.</summary>
        private void DeleteUploadedFile(string? url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/files/uploads/", StringComparison.OrdinalIgnoreCase)) return;
            var path = Path.Combine(_env.WebRootPath, "files", "uploads", Path.GetFileName(url));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        /* ======================== Quizzes ======================== */

        public async Task<IActionResult> Quizzes(int? courseId)
        {
            ViewData["CourseId"] = courseId;
            ViewBag.Courses = await _courses.GetAllAsync();
            return View(await _quizzes.GetAllAsync(courseId));
        }

        public async Task<IActionResult> CreateQuiz(int? courseId)
        {
            await LoadCourseOptions();
            return View("QuizForm", new Quiz { CourseId = courseId ?? 0 });
        }

        public async Task<IActionResult> EditQuiz(int id)
        {
            var quiz = await _quizzes.GetByIdAsync(id);
            if (quiz == null) return NotFound();
            await LoadCourseOptions();
            return View("QuizForm", quiz);
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuiz(Quiz model)
        {
            if (!ModelState.IsValid)
            {
                await LoadCourseOptions();
                return View("QuizForm", model);
            }

            if (model.QuizId == 0)
            {
                var newId = await _quizzes.CreateAsync(model);
                TempData["Success"] = $"Quiz \"{model.Title}\" created. Now add its questions.";
                return RedirectToAction(nameof(Questions), new { quizId = newId });
            }

            await _quizzes.UpdateAsync(model);
            TempData["Success"] = $"Quiz \"{model.Title}\" saved.";
            return RedirectToAction(nameof(Quizzes));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuiz(int id)
        {
            var quiz = await _quizzes.GetByIdAsync(id);
            if (quiz == null) return NotFound();
            await _quizzes.DeleteAsync(id);
            TempData["Success"] = $"Quiz \"{quiz.Title}\" and its questions and attempts were deleted.";
            return RedirectToAction(nameof(Quizzes));
        }

        /* ======================== Questions ======================== */

        public async Task<IActionResult> Questions(int quizId)
        {
            var quiz = await _quizzes.GetByIdAsync(quizId);
            if (quiz == null) return NotFound();
            ViewBag.Quiz = quiz;
            return View(await _quizzes.GetQuestionsAsync(quizId));
        }

        public async Task<IActionResult> CreateQuestion(int quizId)
        {
            var quiz = await _quizzes.GetByIdAsync(quizId);
            if (quiz == null) return NotFound();
            ViewBag.Quiz = quiz;
            return View("QuestionForm", new Question { QuizId = quizId });
        }

        public async Task<IActionResult> EditQuestion(int id)
        {
            var question = await _quizzes.GetQuestionAsync(id);
            if (question == null) return NotFound();
            ViewBag.Quiz = await _quizzes.GetByIdAsync(question.QuizId);
            return View("QuestionForm", question);
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestion(Question model)
        {
            var quiz = await _quizzes.GetByIdAsync(model.QuizId);
            if (quiz == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Quiz = quiz;
                return View("QuestionForm", model);
            }

            if (model.QuestionId == 0) await _quizzes.CreateQuestionAsync(model);
            else await _quizzes.UpdateQuestionAsync(model);

            TempData["Success"] = "Question saved.";
            return RedirectToAction(nameof(Questions), new { quizId = model.QuizId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _quizzes.GetQuestionAsync(id);
            if (question == null) return NotFound();
            await _quizzes.DeleteQuestionAsync(id);
            TempData["Success"] = "Question deleted.";
            return RedirectToAction(nameof(Questions), new { quizId = question.QuizId });
        }

        /* ======================== Discussions & attempts ======================== */

        public async Task<IActionResult> Discussions(string? search)
        {
            ViewData["Search"] = search;
            return View(await _discussions.GetAllAsync(search));
        }

        public async Task<IActionResult> Attempts() => View(await _quizzes.GetRecentAttemptsAsync(200));

        [HttpPost]
        public async Task<IActionResult> DeleteAttempt(int id)
        {
            await _quizzes.DeleteAttemptAsync(id);
            TempData["Success"] = "Quiz attempt deleted.";
            return RedirectToAction(nameof(Attempts));
        }

        /* ======================== Contact messages ======================== */

        public async Task<IActionResult> Messages() => View(await _contact.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> ToggleMessageRead(int id, bool isRead)
        {
            await _contact.SetReadAsync(id, isRead);
            return RedirectToAction(nameof(Messages));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            await _contact.DeleteAsync(id);
            TempData["Success"] = "Message deleted.";
            return RedirectToAction(nameof(Messages));
        }

        /* ======================== helpers ======================== */

        private async Task LoadCategoryOptions() =>
            ViewBag.CategoryOptions = (await _categories.GetAllAsync())
                .Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())).ToList();

        private async Task LoadCourseOptions() =>
            ViewBag.CourseOptions = (await _courses.GetAllAsync())
                .Select(c => new SelectListItem(c.Title, c.CourseId.ToString())).ToList();
    }
}

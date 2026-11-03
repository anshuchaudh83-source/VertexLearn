using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data.Repositories;
using VertexLearn.Infrastructure;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    /// <summary>Public course catalogue — available to registered and non-registered visitors.</summary>
    public class CoursesController : Controller
    {
        private readonly CourseRepository _courses;
        private readonly CategoryRepository _categories;
        private readonly ResourceRepository _resources;
        private readonly QuizRepository _quizzes;
        private readonly EnrollmentRepository _enrollments;
        private readonly DiscussionRepository _discussions;

        public CoursesController(CourseRepository courses, CategoryRepository categories, ResourceRepository resources,
                                 QuizRepository quizzes, EnrollmentRepository enrollments, DiscussionRepository discussions)
        {
            _courses = courses;
            _categories = categories;
            _resources = resources;
            _quizzes = quizzes;
            _enrollments = enrollments;
            _discussions = discussions;
        }

        // GET: /Courses?categoryId=1&search=sql
        public async Task<IActionResult> Index(int? categoryId, string? search)
        {
            var model = new CourseCatalogViewModel
            {
                Courses = await _courses.GetAllAsync(categoryId, search),
                Categories = await _categories.GetAllAsync(),
                SelectedCategoryId = categoryId,
                Search = search
            };
            if (User.IsStudent())
            {
                model.EnrolledCourseIds = await _enrollments.GetEnrolledCourseIdsAsync(User.GetUserId());
            }
            return View(model);
        }

        // GET: /Courses/Details/5 — course overview; materials unlock after enrolling
        public async Task<IActionResult> Details(int id)
        {
            var course = await _courses.GetByIdAsync(id);
            if (course == null) return NotFound();

            var model = new CourseDetailsViewModel
            {
                Course = course,
                Resources = await _resources.GetByCourseAsync(id),
                Quizzes = await _quizzes.GetAllAsync(id),
                IsAdmin = User.IsAdmin(),
                IsEnrolled = User.IsStudent() && await _enrollments.IsEnrolledAsync(User.GetUserId(), id),
                PostCount = await _discussions.CountByCourseAsync(id)
            };
            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data.Repositories;
using VertexLearn.Infrastructure;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    /// <summary>Online self-assessment: take a quiz, get it marked instantly, review the answers.</summary>
    [Authorize]
    public class QuizController : Controller
    {
        private readonly QuizRepository _quizzes;
        private readonly EnrollmentRepository _enrollments;

        public QuizController(QuizRepository quizzes, EnrollmentRepository enrollments)
        {
            _quizzes = quizzes;
            _enrollments = enrollments;
        }

        private async Task<bool> CanAccessCourseAsync(int courseId) =>
            User.IsAdmin() || await _enrollments.IsEnrolledAsync(User.GetUserId(), courseId);

        // GET: /Quiz/Take/3
        public async Task<IActionResult> Take(int id)
        {
            var quiz = await _quizzes.GetByIdAsync(id);
            if (quiz == null) return NotFound();
            if (!await CanAccessCourseAsync(quiz.CourseId))
            {
                TempData["Error"] = "Enrol in the course before taking its quizzes.";
                return RedirectToAction("Details", "Courses", new { id = quiz.CourseId });
            }

            var questions = await _quizzes.GetQuestionsAsync(id);
            if (questions.Count == 0)
            {
                TempData["Error"] = "This quiz has no questions yet.";
                return RedirectToAction("Learn", "Member", new { id = quiz.CourseId });
            }

            return View(new QuizTakeViewModel { Quiz = quiz, Questions = questions });
        }

        // POST: /Quiz/Take/3 — answers arrive as answers[questionId] = "A".."D"
        [HttpPost]
        public async Task<IActionResult> Take(int id, Dictionary<int, string>? answers)
        {
            var quiz = await _quizzes.GetByIdAsync(id);
            if (quiz == null) return NotFound();
            if (!await CanAccessCourseAsync(quiz.CourseId)) return Forbid();

            var questions = await _quizzes.GetQuestionsAsync(id);
            answers ??= new Dictionary<int, string>();

            // Server-side validation: every question must have a valid answer
            var unanswered = questions.Count(q => !answers.TryGetValue(q.QuestionId, out var a) || !new[] { "A", "B", "C", "D" }.Contains(a));
            if (unanswered > 0)
            {
                ModelState.AddModelError(string.Empty, $"Answer all questions before submitting ({unanswered} left).");
                ViewData["Answers"] = answers;
                return View(new QuizTakeViewModel { Quiz = quiz, Questions = questions });
            }

            // Mark on the server (the correct answers are never sent to the browser)
            var marked = questions.Select(q =>
            {
                var selected = answers[q.QuestionId];
                return (q.QuestionId, (string?)selected, selected == q.CorrectOption);
            }).ToList();

            var score = marked.Count(m => m.Item3);
            var attemptId = await _quizzes.SaveAttemptAsync(User.GetUserId(), id, score, questions.Count, marked);

            return RedirectToAction(nameof(Result), new { id = attemptId });
        }

        // GET: /Quiz/Result/12
        public async Task<IActionResult> Result(int id)
        {
            var attempt = await _quizzes.GetAttemptAsync(id);
            if (attempt == null) return NotFound();
            if (attempt.UserId != User.GetUserId() && !User.IsAdmin()) return Forbid();

            return View(new QuizResultViewModel
            {
                Attempt = attempt,
                Answers = await _quizzes.GetAttemptAnswersAsync(id)
            });
        }
    }
}

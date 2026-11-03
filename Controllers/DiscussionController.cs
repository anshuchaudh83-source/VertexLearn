using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertexLearn.Data.Repositories;
using VertexLearn.Infrastructure;
using VertexLearn.Models;
using VertexLearn.Models.ViewModels;

namespace VertexLearn.Controllers
{
    /// <summary>Asynchronous course discussion board. Members manage their own posts; admins moderate all.</summary>
    [Authorize]
    public class DiscussionController : Controller
    {
        private readonly DiscussionRepository _discussions;
        private readonly CourseRepository _courses;
        private readonly EnrollmentRepository _enrollments;

        public DiscussionController(DiscussionRepository discussions, CourseRepository courses, EnrollmentRepository enrollments)
        {
            _discussions = discussions;
            _courses = courses;
            _enrollments = enrollments;
        }

        private async Task<bool> CanAccessCourseAsync(int courseId) =>
            User.IsAdmin() || await _enrollments.IsEnrolledAsync(User.GetUserId(), courseId);

        private bool CanModify(int authorId) => User.IsAdmin() || authorId == User.GetUserId();

        // GET: /Discussion/Index?courseId=1
        public async Task<IActionResult> Index(int courseId)
        {
            var course = await _courses.GetByIdAsync(courseId);
            if (course == null) return NotFound();
            if (!await CanAccessCourseAsync(courseId))
            {
                TempData["Error"] = "Enrol in this course to join its discussion.";
                return RedirectToAction("Details", "Courses", new { id = courseId });
            }

            return View(new DiscussionIndexViewModel
            {
                Course = course,
                Posts = await _discussions.GetByCourseAsync(courseId),
                NewPost = new DiscussionPost { CourseId = courseId }
            });
        }

        // POST: /Discussion/Create  (INSERT into DiscussionPosts)
        [HttpPost]
        public async Task<IActionResult> Create([Bind(Prefix = "NewPost")] DiscussionPost post)
        {
            var course = await _courses.GetByIdAsync(post.CourseId);
            if (course == null) return NotFound();
            if (!await CanAccessCourseAsync(post.CourseId)) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(nameof(Index), new DiscussionIndexViewModel
                {
                    Course = course,
                    Posts = await _discussions.GetByCourseAsync(post.CourseId),
                    NewPost = post
                });
            }

            post.UserId = User.GetUserId();
            var postId = await _discussions.CreatePostAsync(post);
            TempData["Success"] = "Your post is live.";
            return RedirectToAction(nameof(Thread), new { id = postId });
        }

        // GET: /Discussion/Thread/4
        public async Task<IActionResult> Thread(int id)
        {
            var post = await _discussions.GetPostAsync(id);
            if (post == null) return NotFound();
            if (!await CanAccessCourseAsync(post.CourseId)) return RedirectToAction("Details", "Courses", new { id = post.CourseId });

            return View(new ThreadViewModel
            {
                Post = post,
                Replies = await _discussions.GetRepliesAsync(id),
                NewReply = new DiscussionReply { PostId = id }
            });
        }

        // POST: /Discussion/Reply  (INSERT into DiscussionReplies)
        [HttpPost]
        public async Task<IActionResult> Reply([Bind(Prefix = "NewReply")] DiscussionReply reply)
        {
            var post = await _discussions.GetPostAsync(reply.PostId);
            if (post == null) return NotFound();
            if (!await CanAccessCourseAsync(post.CourseId)) return Forbid();

            if (!ModelState.IsValid)
            {
                return View(nameof(Thread), new ThreadViewModel
                {
                    Post = post,
                    Replies = await _discussions.GetRepliesAsync(post.PostId),
                    NewReply = reply
                });
            }

            reply.UserId = User.GetUserId();
            await _discussions.CreateReplyAsync(reply);
            return RedirectToAction(nameof(Thread), "Discussion", new { id = post.PostId }, "replies");
        }

        // GET: /Discussion/Edit/4
        public async Task<IActionResult> Edit(int id)
        {
            var post = await _discussions.GetPostAsync(id);
            if (post == null) return NotFound();
            if (!CanModify(post.UserId)) return Forbid();
            return View(post);
        }

        // POST: /Discussion/Edit/4  (UPDATE DiscussionPosts)
        [HttpPost]
        public async Task<IActionResult> Edit(int id, DiscussionPost model)
        {
            var post = await _discussions.GetPostAsync(id);
            if (post == null) return NotFound();
            if (!CanModify(post.UserId)) return Forbid();

            if (!ModelState.IsValid)
            {
                model.PostId = id;
                model.CourseId = post.CourseId;
                model.CourseTitle = post.CourseTitle;
                return View(model);
            }

            post.Title = model.Title;
            post.Body = model.Body;
            await _discussions.UpdatePostAsync(post);
            TempData["Success"] = "Post updated.";
            return RedirectToAction(nameof(Thread), new { id });
        }

        // POST: /Discussion/Delete/4  (DELETE from DiscussionPosts; replies cascade)
        [HttpPost]
        public async Task<IActionResult> Delete(int id, string? returnTo)
        {
            var post = await _discussions.GetPostAsync(id);
            if (post == null) return NotFound();
            if (!CanModify(post.UserId)) return Forbid();

            await _discussions.DeletePostAsync(id);
            TempData["Success"] = "Post deleted.";
            if (returnTo == "admin" && User.IsAdmin()) return RedirectToAction("Discussions", "Admin");
            return RedirectToAction(nameof(Index), new { courseId = post.CourseId });
        }

        // POST: /Discussion/DeleteReply/7
        [HttpPost]
        public async Task<IActionResult> DeleteReply(int id)
        {
            var reply = await _discussions.GetReplyAsync(id);
            if (reply == null) return NotFound();
            if (!CanModify(reply.UserId)) return Forbid();

            await _discussions.DeleteReplyAsync(id);
            TempData["Success"] = "Reply deleted.";
            return RedirectToAction(nameof(Thread), "Discussion", new { id = reply.PostId }, "replies");
        }
    }
}

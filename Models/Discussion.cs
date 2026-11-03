using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    public class DiscussionPost
    {
        public int PostId { get; set; }
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Give your post a title.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be 5–150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Write your question or comment.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Post must be 10–4000 characters.")]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ReplyCount { get; set; }
    }

    public class DiscussionReply
    {
        public int ReplyId { get; set; }
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Write a reply before posting.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Reply must be 2–2000 characters.")]
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}

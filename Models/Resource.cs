using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VertexLearn.Models
{
    /// <summary>A learning material attached to a course (video, PDF, external link, notes, image or audio).</summary>
    public class Resource
    {
        public static readonly string[] Types = { "Video", "PDF", "Link", "Notes", "Image", "Audio" };

        public int ResourceId { get; set; }

        [Display(Name = "Course")]
        [Range(1, int.MaxValue, ErrorMessage = "Choose a course.")]
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a resource title.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Title must be 3–150 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Choose a resource type.")]
        [RegularExpression("^(Video|PDF|Link|Notes|Image|Audio)$", ErrorMessage = "Choose a valid resource type.")]
        public string Type { get; set; } = "Video";

        // Not [Required]: the admin can either type a URL/path or upload a file (checked in AdminController.SaveResource)
        [Display(Name = "URL or file path")]
        [StringLength(500)]
        [RegularExpression(@"^(https?://|/).+", ErrorMessage = "Use a site path starting with / or a full http(s) URL.")]
        public string Url { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
        public string? Description { get; set; }

        /// <summary>Optional file uploaded from the admin form (PDF, notes, image, audio or video). Not stored in the database — only its saved path goes into Url.</summary>
        [Display(Name = "Or upload a file")]
        public IFormFile? UploadFile { get; set; }

        [Display(Name = "Display order")]
        [Range(0, 999, ErrorMessage = "Order must be between 0 and 999.")]
        public int SortOrder { get; set; }
    }
}

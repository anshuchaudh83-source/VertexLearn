using System.ComponentModel.DataAnnotations;

namespace VertexLearn.Models
{
    public class ContactMessage
    {
        public int MessageId { get; set; }

        [Display(Name = "Your name")]
        [Required(ErrorMessage = "Enter your name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be 2–100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter your email address.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enter a subject.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Subject must be 3–150 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Write your message.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Message must be 10–2000 characters.")]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

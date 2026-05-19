using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int PostId { get; set; }
        public Post? Post { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        
        public IdentityUser? User { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Models
{
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;

        public bool IsFeatured { get; set; } = false;

        public int ReadCount { get; set; } = 0;

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        
        public int? CategoryId { get; set; }
        
        public Category? Category { get; set; }

        public IdentityUser? Author { get; set; }

        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Models;

public class ChatMessage
{
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = null!;
    
    [ForeignKey("SenderId")]
    public virtual IdentityUser? Sender { get; set; }

    [Required]
    public string ReceiverId { get; set; } = null!;

    [ForeignKey("ReceiverId")]
    public virtual IdentityUser? Receiver { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

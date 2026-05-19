using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using BlogApp.Data;
using BlogApp.Models;

namespace BlogApp.Controllers
{
    [Authorize]
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CommentsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Comment comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            if (!string.IsNullOrWhiteSpace(comment.Content))
            {
                comment.UserId = user.Id;
                comment.CreatedAt = DateTime.UtcNow;
                
                _context.Add(comment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Posts", new { id = comment.PostId });
        }
    }
}

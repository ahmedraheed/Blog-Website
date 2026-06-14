using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using Microsoft.AspNetCore.Identity;

namespace BlogApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalPosts = await _context.Posts.CountAsync();
            ViewBag.TotalComments = await _context.Comments.CountAsync();
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();

            var categoryStats = await _context.Categories
                .Select(c => new { Name = c.Name, Count = c.Posts.Count })
                .ToListAsync();
            
            ViewBag.CategoryLabels = System.Text.Json.JsonSerializer.Serialize(categoryStats.Select(c => c.Name));
            ViewBag.CategoryData = System.Text.Json.JsonSerializer.Serialize(categoryStats.Select(c => c.Count));

            var pendingPosts = await _context.Posts
                .Include(p => p.Author)
                .Where(p => !p.IsApproved)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
            ViewBag.PendingPosts = pendingPosts;

            var recentPosts = await _context.Posts
                .Include(p => p.Author)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(recentPosts);
        }
    }
}

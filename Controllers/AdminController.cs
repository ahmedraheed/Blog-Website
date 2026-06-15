using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using Microsoft.AspNetCore.Identity;
using BlogApp.Services;

namespace BlogApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ThemeService _themeService;
        private readonly OnlineUserService _onlineUserService;

        public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, ThemeService themeService, OnlineUserService onlineUserService)
        {
            _context = context;
            _userManager = userManager;
            _themeService = themeService;
            _onlineUserService = onlineUserService;
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

            var topPosts = await _context.Posts
                .Where(p => p.IsApproved)
                .OrderByDescending(p => p.ReadCount)
                .Take(10)
                .Select(p => new { Title = p.Title, Reads = p.ReadCount })
                .ToListAsync();

            ViewBag.ImpressionLabels = System.Text.Json.JsonSerializer.Serialize(topPosts.Select(p => p.Title));
            ViewBag.ImpressionData = System.Text.Json.JsonSerializer.Serialize(topPosts.Select(p => p.Reads));

            var onlineUserIds = _onlineUserService.GetOnlineUsers();
            var onlineUsers = await _userManager.Users
                .Where(u => onlineUserIds.Contains(u.Id))
                .Select(u => new { UserName = u.UserName, Email = u.Email })
                .ToListAsync();
            ViewBag.OnlineUsers = onlineUsers;

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

        [HttpGet]
        public IActionResult ThemeSettings()
        {
            ViewBag.CurrentColor = _themeService.GetPrimaryColor();
            return View();
        }

        [HttpPost]
        public IActionResult ThemeSettings(string primaryColor)
        {
            _themeService.SetPrimaryColor(primaryColor);
            TempData["SuccessMessage"] = "Global theme updated successfully!";
            return RedirectToAction(nameof(ThemeSettings));
        }
    }
}

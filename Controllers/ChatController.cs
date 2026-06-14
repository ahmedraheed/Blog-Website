using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogApp.Data;
using BlogApp.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BlogApp.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ChatController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return NotFound();

        if (await _userManager.IsInRoleAsync(currentUser, "Admin"))
        {
            return RedirectToAction("AdminIndex");
        }

        // Find the admin user
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var admin = admins.FirstOrDefault();
        if (admin == null) return Content("No admin found. Please run the seed data.");

        ViewBag.AdminId = admin.Id;
        ViewBag.CurrentUserId = currentUser.Id;

        var messages = await _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => (m.SenderId == currentUser.Id && m.ReceiverId == admin.Id) || 
                        (m.SenderId == admin.Id && m.ReceiverId == currentUser.Id))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return View(messages);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminIndex(string? userId)
    {
        var adminUser = await _userManager.GetUserAsync(User);
        if (adminUser == null) return NotFound();

        // Get all users the admin has chatted with, or regular users to chat with
        // Let's just list all non-admin users for simplicity
        var users = await _userManager.Users.ToListAsync();
        var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
        var adminIds = adminUsers.Select(a => a.Id).ToList();
        
        var chatUsers = users.Where(u => !adminIds.Contains(u.Id)).ToList();

        ViewBag.Users = chatUsers;
        ViewBag.CurrentUserId = adminUser.Id;
        ViewBag.ActiveUserId = userId;

        var messages = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(userId))
        {
            messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => (m.SenderId == adminUser.Id && m.ReceiverId == userId) || 
                            (m.SenderId == userId && m.ReceiverId == adminUser.Id))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        return View(messages);
    }
}

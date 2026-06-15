using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BlogApp.Data;
using BlogApp.Models;
using System;
using System.Threading.Tasks;

namespace BlogApp.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private readonly BlogApp.Services.OnlineUserService _onlineUserService;

    public ChatHub(ApplicationDbContext context, BlogApp.Services.OnlineUserService onlineUserService)
    {
        _context = context;
        _onlineUserService = onlineUserService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _onlineUserService.UserConnected(userId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId != null)
        {
            _onlineUserService.UserDisconnected(userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(string receiverId, string message)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = message,
            Timestamp = DateTime.UtcNow
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        // Send to receiver
        await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, message);
        
        // Also send back to sender so they see their own message confirmed
        if (senderId != receiverId)
        {
            await Clients.User(senderId).SendAsync("ReceiveMessage", senderId, message);
        }
    }
}

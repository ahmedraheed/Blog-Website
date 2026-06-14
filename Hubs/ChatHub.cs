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

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
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

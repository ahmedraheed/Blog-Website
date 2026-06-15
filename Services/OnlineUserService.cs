using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BlogApp.Services
{
    public class OnlineUserService
    {
        // Tracks UserId -> Number of active connections
        private readonly ConcurrentDictionary<string, int> _onlineUsers = new();

        public void UserConnected(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;
            
            _onlineUsers.AddOrUpdate(userId, 1, (_, count) => count + 1);
        }

        public void UserDisconnected(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            _onlineUsers.AddOrUpdate(userId, 0, (_, count) => count > 0 ? count - 1 : 0);
            
            // Clean up if 0 connections
            if (_onlineUsers.TryGetValue(userId, out var finalCount) && finalCount <= 0)
            {
                _onlineUsers.TryRemove(userId, out _);
            }
        }

        public IEnumerable<string> GetOnlineUsers()
        {
            return _onlineUsers.Keys.ToList();
        }
    }
}

using MessengerServerAtt2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MessengerServerAtt2.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private static readonly Dictionary<Guid, string> _userConnections = new();

        public ChatHub(AppDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var connectionId = Context.ConnectionId;

            _userConnections[userId] = connectionId;

            await UpdateUserStatus(userId, UserStatus.Online);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            _userConnections.Remove(userId);

            await UpdateUserStatus(userId, UserStatus.Offline);

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(Guid dialogId, string content)
        {
            var senderId = GetUserId();

            var message = new Message
            {
                Id = Guid.NewGuid(),
                DialogId = dialogId,
                SenderId = senderId,
                Content = content,
                Type = MessageType.Text,
                Status = MessageStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            var participants = await _context.UserDialogs
                .Where(ud => ud.DialogId == dialogId && ud.UserId != senderId)
                .Select(ud => ud.UserId)
                .ToListAsync();

            foreach (var participantId in participants)
            {
                if (_userConnections.TryGetValue(participantId, out var connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", new
                    {
                        message.Id,
                        message.DialogId,
                        message.SenderId,
                        message.Content,
                        message.SentAt
                    });
                }
            }
        }

        public async Task Typing(Guid dialogId)
        {
            var userId = GetUserId();
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

            var participants = await _context.UserDialogs
                .Where(ud => ud.DialogId == dialogId && ud.UserId != userId)
                .Select(ud => ud.UserId)
                .ToListAsync();

            foreach (var participantId in participants)
            {
                if (_userConnections.TryGetValue(participantId, out var connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("UserTyping", new
                    {
                        dialogId,
                        userId,
                        userName,
                        timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim);
        }

        private async Task UpdateUserStatus(Guid userId, UserStatus status)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Status = status;
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
    }
}
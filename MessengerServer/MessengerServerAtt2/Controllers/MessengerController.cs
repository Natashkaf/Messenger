using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MessengerServerAtt2.Models;
using System.Security.Claims;

namespace MessengerServerAtt2.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MessagesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/messages/dialog/{dialogId}
        [HttpGet("dialog/{dialogId}")]
        public async Task<IActionResult> GetDialogMessages(Guid dialogId)
        {
            var userId = GetUserId();

            // Проверяем, есть ли у пользователя доступ к диалогу
            var hasAccess = await _context.UserDialogs
                .AnyAsync(ud => ud.UserId == userId && ud.DialogId == dialogId);

            if (!hasAccess)
                return Forbid();

            var messages = await _context.Messages
                .Where(m => m.DialogId == dialogId)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.Content,
                    m.Type,
                    m.FileUrl,
                    m.Status,
                    m.SentAt,
                    m.ReadAt,
                    m.IsEdited
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/messages
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = GetUserId();

            // Проверяем доступ к диалогу
            var hasAccess = await _context.UserDialogs
                .AnyAsync(ud => ud.UserId == userId && ud.DialogId == request.DialogId);

            if (!hasAccess)
                return Forbid();

            var message = new Message
            {
                Id = Guid.NewGuid(),
                DialogId = request.DialogId,
                SenderId = userId,
                Content = request.Content,
                Type = request.Type,
                Status = MessageStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message.Id,
                message.SentAt,
                message.Status
            });
        }

        // PUT: api/messages/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> EditMessage(Guid id, [FromBody] EditMessageRequest request)
        {
            var userId = GetUserId();
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != userId)
                return NotFound();

            if (message.IsDeleted)
                return BadRequest(new { message = "Сообщение удалено" });

            message.Content = request.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Сообщение отредактировано" });
        }

        // DELETE: api/messages/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMessage(Guid id)
        {
            var userId = GetUserId();
            var message = await _context.Messages.FindAsync(id);

            if (message == null || message.SenderId != userId)
                return NotFound();

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Сообщение удалено" });
        }

        // POST: api/messages/{id}/read
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = GetUserId();
            var message = await _context.Messages.FindAsync(id);

            if (message == null)
                return NotFound();

            // Проверяем доступ к диалогу
            var hasAccess = await _context.UserDialogs
                .AnyAsync(ud => ud.UserId == userId && ud.DialogId == message.DialogId);

            if (!hasAccess)
                return Forbid();

            message.Status = MessageStatus.Read;
            message.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userId);
        }
    }

    public class SendMessageRequest
    {
        public Guid DialogId { get; set; }
        public string Content { get; set; }
        public MessageType Type { get; set; } = MessageType.Text;
    }

    public class EditMessageRequest
    {
        public string Content { get; set; }
    }
}
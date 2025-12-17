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
    public class DialogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DialogsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyDialogs()
        {
            var userId = GetUserId();

            var dialogs = await _context.UserDialogs
                .Where(ud => ud.UserId == userId)
                .Include(ud => ud.Dialog)
                .ThenInclude(d => d.Messages.OrderByDescending(m => m.SentAt).Take(1))
                .Select(ud => new
                {
                    ud.DialogId,
                    ud.Dialog.Type,
                    ud.Dialog.Name,
                    ud.Dialog.AvatarUrl,
                    ud.Dialog.CreatedAt,
                    ud.Dialog.LastMessageAt,
                    Participants = ud.Dialog.UserDialogs
                        .Where(ud2 => ud2.UserId != userId)
                        .Select(ud2 => new
                        {
                            ud2.UserId,
                            ud2.User.Username,
                            ud2.User.AvatarUrl,
                            ud2.User.Status
                        }),
                    LastMessage = ud.Dialog.Messages
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(dialogs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDialog(Guid id)
        {
            var userId = GetUserId();

            var hasAccess = await _context.UserDialogs
                .AnyAsync(ud => ud.UserId == userId && ud.DialogId == id);

            if (!hasAccess)
                return Forbid();

            var dialog = await _context.Dialogs
                .Where(d => d.Id == id)
                .Select(d => new
                {
                    d.Id,
                    d.Type,
                    d.Name,
                    d.AvatarUrl,
                    d.CreatedAt,
                    d.LastMessageAt,
                    Participants = d.UserDialogs
                        .Select(ud => new
                        {
                            ud.UserId,
                            ud.User.Username,
                            ud.User.AvatarUrl,
                            ud.User.Status,
                            ud.JoinedAt
                        })
                })
                .FirstOrDefaultAsync();

            if (dialog == null)
                return NotFound();

            return Ok(dialog);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePrivateDialog([FromBody] CreatePrivateDialogRequest request)
        {
            var userId = GetUserId();

            var existingDialog = await _context.UserDialogs
                .Where(ud => ud.UserId == userId)
                .SelectMany(ud => ud.Dialog.UserDialogs
                    .Where(ud2 => ud2.UserId == request.UserId && ud.Dialog.Type == DialogType.Private)
                    .Select(ud2 => ud2.Dialog))
                .FirstOrDefaultAsync();

            if (existingDialog != null)
            {
                return Ok(new { dialogId = existingDialog.Id, message = "Диалог уже существует" });
            }

            var dialog = new Dialog
            {
                Id = Guid.NewGuid(),
                Type = DialogType.Private,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Dialogs.AddAsync(dialog);

            await _context.UserDialogs.AddRangeAsync(
                new UserDialog { UserId = userId, DialogId = dialog.Id },
                new UserDialog { UserId = request.UserId, DialogId = dialog.Id }
            );

            await _context.SaveChangesAsync();

            return Ok(new { dialogId = dialog.Id, message = "Диалог создан" });
        }

        [HttpPost("group")]
        public async Task<IActionResult> CreateGroupDialog([FromBody] CreateGroupDialogRequest request)
        {
            var userId = GetUserId();

            var dialog = new Dialog
            {
                Id = Guid.NewGuid(),
                Type = DialogType.Group,
                Name = request.Name,
                AvatarUrl = request.AvatarUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Dialogs.AddAsync(dialog);

            await _context.UserDialogs.AddAsync(
                new UserDialog { UserId = userId, DialogId = dialog.Id }
            );

            foreach (var participantId in request.ParticipantIds)
            {
                await _context.UserDialogs.AddAsync(
                    new UserDialog { UserId = participantId, DialogId = dialog.Id }
                );
            }

            await _context.SaveChangesAsync();

            return Ok(new { dialogId = dialog.Id, message = "Групповой диалог создан" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> LeaveDialog(Guid id)
        {
            var userId = GetUserId();

            var userDialog = await _context.UserDialogs
                .FirstOrDefaultAsync(ud => ud.UserId == userId && ud.DialogId == id);

            if (userDialog == null)
                return NotFound();

            _context.UserDialogs.Remove(userDialog);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Вы вышли из диалога" });
        }

        private Guid GetUserId()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userId);
        }
    }

    public class CreatePrivateDialogRequest
    {
        public Guid UserId { get; set; }
    }

    public class CreateGroupDialogRequest
    {
        public string Name { get; set; }
        public string? AvatarUrl { get; set; }
        public List<Guid> ParticipantIds { get; set; }
    }
}
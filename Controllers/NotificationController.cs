using Microsoft.AspNetCore.Mvc;
using Handover_2.Data;
using Handover_2.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Handover_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;

        public NotificationController(
            ApplicationDbContext context, 
            IHubContext<NotificationHub> hubContext,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerNotification([FromBody] Notification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            _context.Notifications.Add(notification);

            if (!string.IsNullOrEmpty(notification.TargetRole))
            {
                var usersInRole = await _userManager.GetUsersInRoleAsync(notification.TargetRole);
                foreach (var user in usersInRole)
                {
                    var roleNotification = new Notification
                    {
                        Message = notification.Message,
                        UserId = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false,
                        Type = notification.Type  // Corrected property name
                    };
                    _context.Notifications.Add(roleNotification);
                    await _hubContext.Clients.User(user.Id)
                        .SendAsync("ReceiveNotification", notification.Message);
                }
            }
            else
            {
                await _hubContext.Clients.User(notification.UserId)
                    .SendAsync("ReceiveNotification", notification.Message);
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

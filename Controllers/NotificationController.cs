using Microsoft.AspNetCore.Mvc;
using Handover_2.Data;
using Handover_2.Models;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Handover_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationController(
            ApplicationDbContext context, 
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("trigger")]
        public async Task<IActionResult> TriggerNotification([FromBody] Notification notification)
        {
            if (string.IsNullOrEmpty(notification.UserId))
            {
                return BadRequest("UserId is required");
            }

            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            
            _context.Notifications.Add(notification);
            
            await _hubContext.Clients.User(notification.UserId)
                .SendAsync("ReceiveNotification", notification.Message);

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)  // Limit to latest 10 notifications
                .ToListAsync();

            return Ok(notifications);
        }

        [HttpGet("unread/{userId}")]
        public async Task<IActionResult> GetUnreadCount(string userId)
        {
            var count = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

            return Ok(count);
        }

        [HttpPost("markAsRead/{notificationId}")]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            
            if (notification == null)
                return NotFound();

            // Security check - ensure the current user owns this notification
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (notification.UserId != currentUserId)
                return Unauthorized();

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("markAllAsRead/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(string userId)
        {
            // Security check
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != currentUserId)
                return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}

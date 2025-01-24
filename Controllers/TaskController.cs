using Microsoft.AspNetCore.Mvc;
using Handover_2.Data;
using Handover_2.Models;  // Add this line
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Handover_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTask([FromBody] WorkTask task)  // Changed from Task to WorkTask
        {
            _context.Tasks.Add(task);
            
            // Create notification for task assignment
            var notification = new Notification
            {
                TaskId = task.Id,
                UserId = task.AssignedToUserId,
                Type = NotificationType.TaskAssigned,
                Message = $"New task assigned: {task.Title}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            
            await _context.SaveChangesAsync();
            await NotifyUser(task.AssignedToUserId, notification);
            return Ok();
        }

        [HttpPut("update/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] WorkTask task)
        {
            var existingTask = await _context.Tasks.FindAsync(taskId);
            if (existingTask == null) return NotFound();

            existingTask.Status = task.Status;
            existingTask.LastUpdated = DateTime.UtcNow;

            var notification = new Notification
            {
                TaskId = taskId,
                UserId = existingTask.AssignedToUserId,
                Type = NotificationType.TaskUpdated,
                Message = $"Task updated: {existingTask.Title}",
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
            await NotifyUser(existingTask.AssignedToUserId, notification);
            return Ok();
        }

        private async Task NotifyUser(string userId, Notification notification)
        {
            // Add to notification history
            var history = new Handover_2.Models.NotificationHistory  // Fully qualify the class name
            {
                NotificationId = notification.Id,
                UserId = userId,
                ReadAt = null
            };
            _context.Set<Handover_2.Models.NotificationHistory>().Add(history);  // Use Set<T>() to specify the correct type
            await _context.SaveChangesAsync();

            // TODO: Implement real-time notification using SignalR
        }

        [HttpGet("assigned/{userId}")]
        public async System.Threading.Tasks.Task<IActionResult> GetAssignedTasks(string userId)
        {
            var tasks = await _context.Tasks.Where(t => t.AssignedToUserId == userId).ToListAsync();
            return Ok(tasks);
        }
    }
}

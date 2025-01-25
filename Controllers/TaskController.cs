using Microsoft.AspNetCore.Mvc;
using Handover_2.Data;
using Handover_2.Models;  // This contains all models including Notification
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging; // Add this

namespace Handover_2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext; // Add this
        private readonly ILogger<TaskController> _logger; // Add this

        public TaskController(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, ILogger<TaskController> logger) // Add logger parameter
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger; // Initialize logger
        }

        [HttpPost("assign")]
        public async Task<IActionResult> AssignTask([FromBody] Models.WorkTask task)  // Remove Data. prefix
        {
            if (task == null || string.IsNullOrEmpty(task.AssignedToUserId))
                return BadRequest("Invalid task data");

            try 
            {
                _logger.LogInformation("Assigning task to user: {AssignedToUserId}", task.AssignedToUserId); // Add logging
                task.CreatedAt = DateTime.UtcNow;
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
                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task"); // Add logging
                return StatusCode(500, "Error creating task");
            }
        }

        [HttpPut("update/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] Models.WorkTask task)  // Remove Data. prefix
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

            // Send real-time notification
            _logger.LogInformation("Sending notification to user: {UserId}", userId); // Add logging
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification.Message);
        }

        [HttpGet("assigned/{userId}")]
        public async Task<IActionResult> GetAssignedTasks(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("Invalid user ID");

            try
            {
                var tasks = await _context.Tasks
                    .Where(t => t.AssignedToUserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
                    
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error retrieving tasks");
            }
        }
    }
}

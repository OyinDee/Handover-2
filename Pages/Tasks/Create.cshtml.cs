using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Handover_2.Data;
using Handover_2.Models;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Handover_2.Pages.Tasks
{
    [Authorize(Roles = "Supervisor")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            ApplicationDbContext context, 
            IHubContext<NotificationHub> hubContext,
            UserManager<IdentityUser> userManager,
            ILogger<CreateModel> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public Models.WorkTask TaskItem { get; set; } = new Models.WorkTask 
        { 
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        public SelectList UserList { get; set; }

        [TempData]
        public string Success { get; set; }

        [TempData]
        public string Error { get; set; }

        public async Task OnGetAsync()
        {
            try 
            {
                // Get all users
                var allUsers = await _userManager.Users.ToListAsync();
                var userList = new List<UserViewModel>();

                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var isSupervisor = roles.Contains("Supervisor");
                    
                    // Only add users who are not supervisors
                    if (!isSupervisor)
                    {
                        userList.Add(new UserViewModel
                        {
                            Id = user.Id,
                            DisplayName = $"{user.UserName} ({user.Email})"
                        });
                        _logger.LogInformation($"Added user to dropdown: {user.UserName} ({user.Id})");
                    }
                }

                UserList = new SelectList(userList, "Id", "DisplayName");
                _logger.LogInformation($"Loaded {userList.Count} non-supervisor users for assignment");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading users: {ex.Message}");
                UserList = new SelectList(Enumerable.Empty<UserViewModel>());
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                // Log the received form data
                _logger.LogInformation("Received form data: {@TaskData}", new
                {
                    Title = TaskItem.Title,
                    AssignedToUserId = TaskItem.AssignedToUserId,
                    DueDate = TaskItem.DueDate,
                    Priority = TaskItem.Priority
                });

                // Verify the user ID is valid and not a supervisor
                var assignedUser = await _userManager.FindByIdAsync(TaskItem.AssignedToUserId);
                if (assignedUser == null)
                {
                    _logger.LogError($"Invalid user ID received: {TaskItem.AssignedToUserId}");
                    Error = "Invalid user selection.";
                    await LoadUserList(); // Reload the user list
                    return Page();
                }

                var isSupervisor = await _userManager.IsInRoleAsync(assignedUser, "Supervisor");
                if (isSupervisor)
                {
                    _logger.LogError($"Cannot assign task to supervisor: {assignedUser.UserName}");
                    Error = "Tasks cannot be assigned to supervisors.";
                    await LoadUserList();
                    return Page();
                }

                _logger.LogInformation($"Valid non-supervisor user found: {assignedUser.UserName} (ID: {assignedUser.Id})");

                // Remove navigation property from validation
                if (ModelState.ContainsKey("TaskItem.AssignedToUser"))
                {
                    ModelState.Remove("TaskItem.AssignedToUser");
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    _logger.LogWarning("Model state is invalid: {Errors}", errors);
                    
                    Error = "Please check your input and try again.";
                    var users = await _userManager.Users.ToListAsync();
                    UserList = new SelectList(users, "Id", "UserName");
                    return Page();
                }

                // Log task creation attempt
                _logger.LogInformation("Attempting to save task to database: {@TaskItem}", new
                {
                    TaskItem.Title,
                    TaskItem.AssignedToUserId,
                    TaskItem.DueDate,
                    TaskItem.Priority,
                    TaskItem.Status
                });

                // Add and save the task
                _context.Tasks.Add(TaskItem);
                
                try
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Successfully saved task with ID: {TaskItem.Id}");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error while saving task");
                    Error = "Database error occurred while saving the task.";
                    await LoadUserList();
                    return Page();
                }

                _logger.LogInformation("Task created successfully with ID: {TaskId}", TaskItem.Id);

                // Create and save notification
                var notification = new Notification
                {
                    Title = $"New Task: {TaskItem.Title}",
                    Message = $"New task assigned: {TaskItem.Title}",
                    UserId = TaskItem.AssignedToUserId,
                    TaskId = TaskItem.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Type = NotificationType.TaskAssigned
                };

                _logger.LogInformation("Creating notification: {@Notification}", new 
                {
                    notification.Title,
                    notification.Message,
                    notification.UserId,
                    notification.TaskId,
                    notification.Type
                });

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Send real-time notification
                try
                {
                    await _hubContext.Clients.User(TaskItem.AssignedToUserId)
                        .SendAsync("ReceiveNotification", new
                        {
                            message = notification.Message,
                            taskId = TaskItem.Id,
                            createdAt = notification.CreatedAt
                        });
                    
                    _logger.LogInformation($"Notification sent to user {TaskItem.AssignedToUserId} for task {TaskItem.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error sending notification: {ex.Message}");
                }

                Success = $"Task '{TaskItem.Title}' has been created successfully and assigned to {assignedUser.UserName}.";
                
                // Instead of redirecting, return the same page
                await LoadUserList(); // Reload the user list
                ModelState.Clear(); // Clear the form
                TaskItem = new Models.WorkTask { Status = "Pending", CreatedAt = DateTime.UtcNow }; // Reset the form
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task: {Message}", ex.Message);
                Error = "There was an error creating the task. Please try again.";
                await LoadUserList();
                return Page();
            }
        }

        private async Task LoadUserList()
        {
            var users = await _userManager.Users.ToListAsync();
            var nonSupervisorUsers = new List<UserViewModel>();

            foreach (var user in users)
            {
                if (!await _userManager.IsInRoleAsync(user, "Supervisor"))
                {
                    nonSupervisorUsers.Add(new UserViewModel                    {                        Id = user.Id,                        DisplayName = $"{user.UserName} ({user.Email})"                    });                }            }            UserList = new SelectList(nonSupervisorUsers, "Id", "DisplayName");        }        public class UserViewModel        {            public string Id { get; set; }            public string DisplayName { get; set; }        }    }}

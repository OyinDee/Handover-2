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
        public WorkTask TaskItem { get; set; }

        public SelectList UserList { get; set; }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            UserList = new SelectList(users, "Id", "UserName");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                _context.Tasks.Add(TaskItem);
                await _context.SaveChangesAsync();

                var notification = new Notification
                {
                    Message = $"New task assigned to you: {TaskItem.Title}",
                    UserId = TaskItem.AssignedToUserId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.User(TaskItem.AssignedToUserId)
                    .SendAsync("ReceiveNotification", notification.Message);

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating task: {ex.Message}");
                // Handle error (e.g., show an error message to the user)
                return Page();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Handover_2.Data;
using Handover_2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Handover_2.Pages.Tasks
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, UserManager<IdentityUser> userManager, ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<WorkTask> MyTasks { get; set; } = new List<WorkTask>();

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            _logger.LogInformation($"Getting tasks for user ID: {user.Id}");

            var isSupervisor = await _userManager.IsInRoleAsync(user, "Supervisor");
            _logger.LogInformation($"User {user.UserName} is supervisor: {isSupervisor}");

            var query = _context.Tasks.AsQueryable();

            if (!isSupervisor)
            {
                query = query.Where(t => t.AssignedToUserId == user.Id);
                _logger.LogInformation($"Filtering tasks for non-supervisor user: {user.Id}");
            }

            MyTasks = await query
                .Include(t => t.AssignedToUser)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            _logger.LogInformation($"Retrieved {MyTasks.Count} tasks");

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsDoneAsync(int taskId)
        {
            var user = await _userManager.GetUserAsync(User);
            var task = await _context.Tasks.FindAsync(taskId);

            if (task == null || task.AssignedToUserId != user.Id)
                return NotFound();

            task.Status = "Completed";
            task.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }
}

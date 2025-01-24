using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Handover_2.Data;
using Handover_2.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Handover_2.Pages.Tasks
{
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

        public IList<WorkTask> TaskList { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                TaskList = await _context.Tasks
                    .Where(t => t.AssignedToUserId == userId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching tasks: {ex.Message}");
                // Handle error (e.g., show an error message to the user)
            }
        }
    }
}

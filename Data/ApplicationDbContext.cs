using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Handover_2.Models;  // Add this line

namespace Handover_2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add DbSet properties for Notifications and Tasks
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<WorkTask> Tasks { get; set; }  // Renamed from Task to WorkTask
        public DbSet<NotificationHistory> NotificationHistories { get; set; }  // Corrected property name
    }
    public class WorkTask  // Renamed from Task to WorkTask
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string AssignedToUserId { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
        public string CreatedByUserId { get; set; }
        public string Status { get; set; } = "Pending";
        public string? FeedbackComments { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    public class NotificationHistory
    {
        public int Id { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public bool IsRead { get; set; }
        public string Type { get; set; }
        public int? TaskId { get; set; }
    }
}
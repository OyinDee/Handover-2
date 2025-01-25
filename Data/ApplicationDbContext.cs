using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Handover_2.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Handover_2.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly ILogger<ApplicationDbContext> _logger;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            ILogger<ApplicationDbContext> logger)
            : base(options)
        {
            _logger = logger;
        }

        public DbSet<WorkTask> Tasks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationHistory> NotificationHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add index for faster user lookups
            modelBuilder.Entity<IdentityUser>()
                .HasIndex(u => u.NormalizedUserName)
                .HasDatabaseName("UserNameIndex")
                .IsUnique();

            // Configure WorkTask
            modelBuilder.Entity<WorkTask>(entity =>
            {
                entity.ToTable("Tasks");
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(4000);
                    
                entity.Property(e => e.AssignedToUserId)
                    .IsRequired()
                    .HasMaxLength(450);
                    
                entity.Property(e => e.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Pending");
                    
                entity.Property(e => e.Priority)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasOne(t => t.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedToUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Notification
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Message)
                    .IsRequired()
                    .HasMaxLength(4000);

                entity.HasOne(n => n.Task)
                    .WithMany()
                    .HasForeignKey(n => n.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure NotificationHistory
            modelBuilder.Entity<NotificationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(n => n.Notification)
                    .WithMany()
                    .HasForeignKey(n => n.NotificationId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(n => n.Task)
                    .WithMany()
                    .HasForeignKey(n => n.TaskId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                    .ToList();

                foreach (var entry in entries)
                {
                    _logger.LogInformation("Saving changes for entity: {@Entity}", new
                    {
                        Type = entry.Entity.GetType().Name,
                        State = entry.State.ToString(),
                        Properties = entry.CurrentValues.Properties
                            .Select(p => new { Name = p.Name, Value = entry.CurrentValues[p] })
                    });
                }

                return await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SaveChangesAsync");
                throw;
            }
        }
    }
}
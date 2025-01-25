using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Handover_2.Data;

namespace Handover_2.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string Message { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        public int TaskId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; }

        public NotificationType Type { get; set; }

        [ForeignKey("TaskId")]
        public virtual WorkTask Task { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
    }

    public class NotificationHistory
    {
        [Key]
        public int Id { get; set; }

        public int NotificationId { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; }

        public int? TaskId { get; set; }

        public DateTime? ReadAt { get; set; }

        [ForeignKey("NotificationId")]
        public virtual Notification Notification { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        [ForeignKey("TaskId")]
        public virtual WorkTask Task { get; set; }
    }

    public enum NotificationType
    {
        TaskAssigned,
        TaskUpdated,
        TaskCompleted
    }
}
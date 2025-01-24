using System;
using Handover_2.Data;  // Add this line

namespace Handover_2.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public bool IsRead { get; set; }
        public string? TargetRole { get; set; }
        public NotificationType Type { get; set; }  // Use the enum instead of string
        public int TaskId { get; set; }  // Required TaskId for task relationship
        
        // Navigation property
        public virtual WorkTask Task { get; set; }
    }

    public class NotificationHistory
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; }
        public bool IsRead { get; set; }
        public NotificationType Type { get; set; }
        public int TaskId { get; set; }  // Changed from nullable to required
        public DateTime? ReadAt { get; set; }  // Added ReadAt property
        
        // Navigation properties
        public virtual Notification Notification { get; set; }
        public virtual WorkTask Task { get; set; }
    }

    public enum NotificationType
    {
        TaskAssigned,
        TaskUpdated,
        TaskCompleted,
        Feedback,
        Reminder,
        Other
    }
}
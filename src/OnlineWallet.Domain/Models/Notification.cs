using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Domain.Enums;
namespace OnlineWallet.Domain.Models
{
    /// <summary>
    /// Represents a notification sent to a user.
    /// Used for system alerts, transaction updates, and user communications.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Unique identifier for the notification.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Identifier of the user receiving the notification.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Content of the notification message.
        /// </summary>
        public string Message { get; set; }

        // <summary>
        /// Timestamp when the notification was created.
        /// </summary>
        public DateTime ActionTime { get; private set; }

        /// <summary>
        /// Title or subject of the notification.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Type of notification (alert, Deposit, Transfer).
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// Creates a new notification.
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="userId">Recipient user identifier</param>
        /// <param name="message">Notification content</param>
        /// <param name="actionTime">Creation timestamp (in UTC)</param>
        /// <param name="title">Notification title</param>
        /// <param name="type">Notification type</param>
        public Notification(Guid id, Guid userId, string message, DateTime actionTime, string title, NotificationType type)
        {
            Id = id;
            UserId = userId;
            Message = message;
            ActionTime = actionTime;
            Title = title;
            Type = type;
        }
    }
}

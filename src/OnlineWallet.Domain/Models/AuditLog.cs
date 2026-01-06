using OnlineWallet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineWallet.Domain.Models
{
    public class AuditLog
    {
        /// <summary>
        /// Records system activities and transactions for auditing purposes.
        /// Tracks who performed what action and when.
        /// </summary>
        [Key]
        public Guid Id { get; private set; }

        /// <summary>
        /// The ID of the user who performed the action being logged.
        /// </summary>
        /// <remarks>
        /// Foreign key to the User entity. Null for system-generated actions.
        /// </remarks>
        [ForeignKey("User")]
        public Guid? PerformedBy { get; private set; }

        /// <summary>
        /// Identifier of the user who performed the action.
        /// Null if action performed by anonymous user. e.g. unknown user registration.
        /// </summary>
        [Required]
        public AuditLogActionType ActionType { get; private set; }

        /// <summary>
        /// Timestamp when the action occurred.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Additional details about the action.
        /// </summary>
        public string? Details { get; private set; }

        /// <summary>
        /// Status of the logged action.
        /// </summary>
        [Required]
        public AuditLogStatus Status { get; private set; }


        /// <summary>
        /// Creates a new audit log entry.
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="actionType">Type of action performed</param>
        /// <param name="createdAt">Timestamp of the action</param>
        /// <param name="status">Status of the action</param>
        /// <param name="performedBy">User who performed the action (optional)</param>
        /// <param name="details">Additional action details (optional)</param>
        public AuditLog(Guid id, AuditLogActionType actionType,
            DateTime createdAt, AuditLogStatus status, Guid? performedBy = null, string? details=null) {
            Id = id;
            PerformedBy = performedBy;
            ActionType = actionType;
            CreatedAt = createdAt;
            Details = details;
            Status = status;
        }


    }
}

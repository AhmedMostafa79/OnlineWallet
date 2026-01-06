using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Interfaces
{
    /// <summary>
    /// Service interface for sending user notifications.
    /// TODO: Implement notification functionality (email/SMS/push notifications).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a deposit alert notification to a user.
        /// TODO: Implement deposit notification logic.
        /// </summary>
        public Task SendDepositAlertAsync(Guid userId, Guid transactionId, decimal amount);

        /// <summary>
        /// Sends a transfer alert notification to a user.
        /// TODO: Implement transfer notification logic.
        /// </summary>
        public Task SendTransferAlertAsync(Guid userId, Guid transactionId, decimal amount);

        /// <summary>
        /// Sends a custom alert notification to a user.
        /// TODO: Implement custom notification logic.
        /// </summary>
        public Task SendCustomAlertAsync(Guid userId, string message);
    }
}
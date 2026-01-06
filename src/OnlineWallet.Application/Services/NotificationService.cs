using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Domain.Models;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// Default implementation of notification service for development/testing.
    /// TODO: Replace with actual notification implementation (email/SMS/push).
    /// Writes notifications to console for debugging purposes.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly ICustomerService _customerService;

        /// <summary>
        /// Initializes a new instance of NotificationService.
        /// </summary>
        /// <param name="customerService">Service for customer operations</param>
        public NotificationService(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        /// <summary>
        /// Sends a deposit alert notification to console.
        /// TODO: Implement actual deposit notification (email/SMS/push).
        /// </summary>
        public async Task SendDepositAlertAsync(Guid userId, Guid transactionId, decimal amount)
        {
            // TODO: Get user details and send actual notification
            // var user = await _customerService.GetCustomerByIdAsync(userId);
            Console.WriteLine($"DEPOSIT ALERT: User {userId} deposited {amount:C} to your account");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends a transfer alert notification to console.
        /// TODO: Implement actual transfer notification (email/SMS/push).
        /// </summary>
        public async Task SendTransferAlertAsync(Guid userId, Guid transactionId, decimal amount)
        {
            // TODO: Implement actual notification delivery
            Console.WriteLine($"TRANSFER ALERT: \n Dear Customer Your transfer of {amount:C} has been successfully processed \nTransaction Id: {transactionId}");
            await Task.CompletedTask;
        }

        /// <summary>
        /// Sends a custom alert notification to console.
        /// TODO: Implement actual custom notification (email/SMS/push).
        /// </summary>
        public async Task SendCustomAlertAsync(Guid userId, string message)
        {
            // TODO: Implement actual notification delivery
            Console.WriteLine($"CUSTOM ALERT: \n Dear Customer {userId}\n{message}");
            await Task.CompletedTask;
        }
    }
}
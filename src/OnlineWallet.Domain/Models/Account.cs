using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OnlineWallet.Domain.Enums;
namespace OnlineWallet.Domain.Models
{
    /// <summary>
    /// Represents a user's financial account in the wallet system.
    /// Manages account balance and transaction operations.
    /// </summary>
    public class Account
    {
        /// <summary>
        /// Unique identifier for the account.
        /// </summary>
        [Key]
        public Guid AccountNumber { get; private set; }

        /// <summary>
        /// Indicates if the account is active or not.
        /// Can be updated by administrators to activate/deactivate accounts.
        /// </summary>
        [Required]
        public bool IsActive { get;  set; }

        /// <summary>
        /// ID of the account owner. Foreign key to User.
        /// </summary>
        [Required]
        [ForeignKey("User")]
        public Guid OwnerId { get; private set; }

        /// <summary>
        /// Current balance in the account. Cannot be negative.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal Balance { get; private set; } = 0;

        /// <summary>
        /// Timestamp when the account was created.
        /// </summary>
        [Required]
        public DateTime CreatedAt { get; private  set; }

        /// <summary>
        /// Creates a new account with initial settings.
        /// </summary>
        /// <param name="accountNumber">Unique account identifier</param>
        /// <param name="isActive">Initial active status</param>
        /// <param name="ownerId">Owner's user identifier</param>
        /// <param name="createdAt">Account creation timestamp (should be UTC)</param>
        public Account(Guid accountNumber,bool isActive, Guid ownerId, DateTime createdAt)
        {
            AccountNumber = accountNumber;
            IsActive = isActive;
            OwnerId = ownerId;
            CreatedAt = createdAt;
        }

        /// <summary>
        /// Validates that an amount is positive.
        /// </summary>
        private void ValidateAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException($"Amount {amount} must be positive!");
            }
        }

        /// <summary>
        /// Validates sufficient funds are available for withdrawal.
        /// </summary>
        private void ValidateSufficientFunds(decimal amount)
        {
            if (amount>Balance)
            {
                throw new InvalidOperationException("Insufficient funds.");
            }
        }

        /// <summary>
        /// Withdraws funds from the account's balance.
        /// </summary>
        /// <param name="amount">Amount to withdraw</param>
        /// <exception cref="ArgumentException">Thrown when amount is not positive</exception>
        /// <exception cref="InvalidOperationException">Thrown when insufficient funds</exception>
        public void Withdraw(decimal amount){
            ValidateAmount(amount);
            ValidateSufficientFunds(amount);
            Balance -= amount;

        }

        /// <summary>
        /// Deposits funds into the account.
        /// </summary>
        /// <param name="amount">Amount to deposit</param>
        /// <exception cref="ArgumentException">Thrown when amount is not positive</exception>
        public void Deposit(decimal amount)
        {
            ValidateAmount(amount);
            Balance += amount;
        }

    }
}

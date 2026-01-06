using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.DTOs
{
    /// <summary>
    /// Data transfer object for account information.
    /// Used to return account details in API responses.
    /// </summary>
    public class GetAccountDto
    {
        /// <summary>
        /// Unique account identifier.
        /// </summary>
        public Guid AccountNumber { get; private set; }

        /// <summary>
        /// Indicates if the account is active or not.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Identifier of the account owner.
        /// </summary>
        public Guid OwnerId { get; private set; }

        /// <summary>
        /// Current account balance.
        /// </summary>
        public decimal Balance { get; private set; } = 0;

        /// <summary>
        /// Date when the account was opened.
        /// </summary>
        public DateTime DateOpened { get; private set; }

        /// <summary>
        /// Creates a new GetAccountDto with account details.
        /// </summary>
        /// <param name="accountNumber">Unique account identifier</param>
        /// <param name="isActive">Account active status</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="balance">Current balance</param>
        /// <param name="dateOpened">Account opening date</param>
        public GetAccountDto(Guid accountNumber, bool isActive, Guid ownerId, decimal balance, DateTime dateOpened)
        {
            AccountNumber = accountNumber;
            IsActive = isActive;
            OwnerId = ownerId;
            Balance = balance;
            DateOpened = dateOpened;
        }
    }
}

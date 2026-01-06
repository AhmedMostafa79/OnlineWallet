using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace OnlineWallet.Application.DTOs
{
    /// <summary>
    /// Data transfer object for creating a new account.
    /// Used in account creation requests.
    /// </summary>
    public class CreateAccountDto 
    {
        /// <summary>
        /// Identifier of the account owner.
        /// </summary>
        [Required]
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Account creation timestamp.
        /// Defaults to current UTC time.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

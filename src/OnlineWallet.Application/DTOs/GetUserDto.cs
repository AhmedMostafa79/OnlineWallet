using OnlineWallet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.DTOs
{
    /// <summary>
    /// Data transfer object for user information.
    /// Used to return user details in API responses.
    /// </summary>
    public class GetUserDto
    {
        /// <summary>
        /// Unique user identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// User's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// User's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// User's current primary account number.
        /// </summary>
        public Guid? CurrentAccountNumber { get; set; }

        /// <summary>
        /// User's role in the system.
        /// </summary>
        public UserRole Role { get; set; }

        /// <summary>
        /// User's date of birth.
        /// </summary>
        public DateOnly DateOfBirth { get; set; }

        /// <summary>
        /// Timestamp when user was created.
        /// </summary>
        public DateTime DateCreated { get; set; }
    }
}
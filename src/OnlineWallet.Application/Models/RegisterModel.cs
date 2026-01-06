using OnlineWallet.Application.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Models
{
    /// <summary>
    /// User registration model.
    /// Contains data required to create a new user account.
    /// </summary>
    public class RegisterModel
    {
        /// <summary>
        /// User's email address.
        /// </summary>
        [Required]
        public string Email { get; set; }

        /// <summary>
        /// User's password.
        /// Must meet strong password requirements.
        /// </summary>
        [Required]
        [StrongPassword]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        /// <summary>
        /// User's first name.
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// User's last name.
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// User's phone number.
        /// </summary>
        [Required]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// User's date of birth.
        /// </summary>
        public DateOnly DateOfBirth { get; set; }
    }
}

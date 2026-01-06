using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Application.Validation
{
    /// <summary>
    /// Validates that a password meets strong security requirements.
    /// Enforces minimum length and character composition rules.
    /// </summary>
    public class StrongPasswordAttribute:ValidationAttribute
    {
        /// <summary>
        /// Minimum required password length.
        /// Default value is 8 characters.
        /// </summary>
        public int MinimumLength { get; set; } = 8;

        /// <summary>
        /// Determines whether the specified value is a valid strong password.
        /// </summary>
        /// <param name="value">Password value to validate</param>
        /// <returns>True if password meets all security requirements, false otherwise</returns>
        public override bool IsValid(object value)
        {
            if (value is not string password)
                return false;
            return password.Length >= MinimumLength &&
                password.Any(char.IsLower) &&
                password.Any(char.IsUpper) &&
                password.Any(char.IsDigit) &&
                password.Any(ch => !char.IsLetterOrDigit(ch));

        }

        /// <summary>
        /// Formats the error message to display password requirements.
        /// </summary>
        /// <param name="name">Name of the field being validated</param>
        /// <returns>Formatted error message describing password requirements</returns>
        public override string FormatErrorMessage(string name)
        {
            return $"Password must be at least {MinimumLength} characters and contain at least one uppercase letter, one lowercase letter, one number, and one special character";
        }
    }
}

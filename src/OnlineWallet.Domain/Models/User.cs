using OnlineWallet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace OnlineWallet.Domain.Models
{
    /// <summary>
    /// Base user entity representing all users in the Online Wallet system.
    /// Implements domain validation rules and business logic for user data.
    /// </summary>
    /// <remarks>
    /// This is an aggregate root entity that enforces business rules for user creation and modification.
    /// Uses Domain-Driven Design principles with encapsulated validation logic.
    /// </remarks>
    public class User
    {
        private string _firstName;
        private string _lastName;

        /// <summary>
        /// Minimum age required for wallet registration
        /// </summary>
        public const int MinimumAge = 18;

        /// <summary>
        /// Unique identifier for the user
        /// </summary>
        [Key]
        public Guid Id { get; private set; }

        /// <summary>
        /// User's first name with automatic trimming and validation
        /// </summary>
        [Required]
        public string FirstName { 
            get=>_firstName;
            set
            {
                ValidateName(value, nameof(FirstName));
                _firstName = value?.Trim();
            } 
        }

        /// <summary>
        /// User's last name with automatic trimming and validation
        /// </summary>
        [Required]
        public string LastName
        {
            get => _lastName;
            set
            {
                ValidateName(value, nameof(LastName));
                _lastName = value?.Trim();
            }
        }
        /// <summary>
        /// User's email address.
        /// Uses Gmail-style validation regex pattern.
        /// </summary>
        [Required]
        public string Email { get; private set; }

        /// <summary>
        /// User's phone number. Must be exactly 11 digits.
        /// </summary>
        [Required]
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Hashed password using BCrypt or similar algorithm
        /// </summary>
        [Required]
        public string PasswordHash { get; private set; }

        /// <summary>
        /// User's date of birth. Used for age verification (minimum 18 years).
        /// </summary>
        [Required]
        public DateOnly DateOfBirth { get; private set; }

        /// <summary>
        /// Timestamp when user was created in UTC
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Current wallet account number associated with the user
        /// </summary>
        public Guid? CurrentAccountNumber { get; set; }

        /// <summary>
        /// User's role in the system (Customer, Admin)
        /// </summary>
        [Required]
        public UserRole Role { get; set; }

        /// <summary>
        /// Initializes a new instance of the User class with full validation
        /// </summary>
        /// <param name="id">Unique identifier</param>
        /// <param name="email">Valid email address</param>
        /// <param name="passwordHash">Hashed password string</param>
        /// <param name="firstName">First name (min 2 chars)</param>
        /// <param name="lastName">Last name (min 2 chars)</param>
        /// <param name="phoneNumber">11-digit phone number</param>
        /// <param name="dateOfBirth">Date of birth (must be 18+ years)</param>
        /// <param name="role">User role</param>
        public User(Guid id, string email, string passwordHash, string firstName,
           string lastName, string phoneNumber, DateOnly dateOfBirth,
           UserRole role)
        {
            // Validate all inputs in constructor
            ValidateUserAge(dateOfBirth);
            ValidateEmail(email);
            ValidatePhoneNumber(phoneNumber);
            ValidateName(firstName.Trim(),nameof(firstName));
            ValidateName(lastName.Trim(),nameof(lastName));
            Id = id;    
            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Email = email.Trim().ToLower();
            PasswordHash = passwordHash;
            PhoneNumber=phoneNumber.Trim();
            DateOfBirth = dateOfBirth;
            CreatedAt =DateTime.UtcNow;
            Role = role;
        }
        /// <summary>
        /// Validates email format using Gmail-style regex pattern
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <exception cref="ArgumentException">Thrown when email is invalid</exception>
        private void ValidateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty");

            // This regex matches Gmail's acceptance criteria
            string pattern = @"^[a-zA-Z0-9](?:[a-zA-Z0-9.+-]*[a-zA-Z0-9])?@[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)+$";

            if (!Regex.IsMatch(email.Trim(), pattern))
                throw new ArgumentException("Invalid email format");

            // Additional length checks
            var parts = email.Split('@');
            if (parts[0].Length > 64) 
                throw new ArgumentException("Email local part too long");
            if (parts[1].Length > 255)
                throw new ArgumentException("Email domain too long");
        }

        /// <summary>
        /// Validates phone number is exactly 11 digits
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <exception cref="ArgumentException">Thrown when phone number is invalid</exception>
        private void ValidatePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone Number cannot be empty");

                var cleaned = new string(phoneNumber.Where(char.IsDigit).ToArray());
            int  phoneNumberLength = 11;
                if ((cleaned.Length!= phoneNumberLength))
                    throw new ArgumentException("Phone number must be exactly 11 digits");
        }
        /// <summary>
        /// Validates name fields (first and last name)
        /// </summary>
        /// <param name="name">Name to validate</param>
        /// <param name="fieldName">Field name for error messages</param>
        /// <exception cref="ArgumentException">Thrown when name is invalid</exception>
        private void ValidateName(string name, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException($"{fieldName} cannot be empty");

            if (name.Trim().Length < 2)
                throw new ArgumentException($"{fieldName} must be at least 2 characters long");
        }
        /// <summary>
        /// Validates user is at least MinimumAge (18) years old
        /// </summary>
        /// <param name="dateOfBirth">Date of birth to validate</param>
        /// <exception cref="InvalidOperationException">Thrown when age is below minimum</exception>
        private void ValidateUserAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - dateOfBirth.Year;

            // Adjust age if birthday hasn't occurred this year
            if (dateOfBirth > today.AddYears(-age))
                age--;

            if (age < MinimumAge)
                throw new InvalidOperationException($"User must be at least {MinimumAge} years old");
        }
    }
}

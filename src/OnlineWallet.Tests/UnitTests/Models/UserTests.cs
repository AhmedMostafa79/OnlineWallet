using FluentAssertions;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace OnlineWallet.Domain.Tests.Models
{
    public class UserTests
    {
        private readonly Guid _testId = Guid.NewGuid();
        private readonly string _validEmail = "test@example.com";
        private readonly string _validPasswordHash = "hashed_password";
        private readonly string _validFirstName = "John";
        private readonly string _validLastName = "Doe";
        private readonly string _validPhoneNumber = "01234567890";//exactly 11 digits
        private readonly DateOnly _validDateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25));
        private readonly UserRole _validRole = UserRole.Customer;


        [Fact]
        public void Constructor_WithValidParameters_CreatesUserSuccessfully()
        {
            // Act
            var user = new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            // Assert
            user.Should().NotBeNull();
            user.Id.Should().Be(_testId);
            user.Email.Should().Be(_validEmail.Trim().ToLower());
            user.PasswordHash.Should().Be(_validPasswordHash);
            user.FirstName.Should().Be(_validFirstName.Trim());
            user.LastName.Should().Be(_validLastName.Trim());
            user.PhoneNumber.Should().Be(_validPhoneNumber.Trim());
            user.DateOfBirth.Should().Be(_validDateOfBirth);
            user.Role.Should().Be(_validRole);
            user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("      ")]
        public void Constructor_WithInvalidEmail_ThrowArgumentException(string invalidEmail)
        {
            
            //Act
            Action act=()=>new User(
                _testId,
                email:invalidEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Email cannot be empty*");
        }

        [Theory]
        [InlineData("garbage")]
        [InlineData("ahmed@gmail")]
        [InlineData("test@.com")]
        [InlineData("@example.com")]
        [InlineData("test@example.")]
        [InlineData("test example@domain.com")]
        [InlineData("test@domain..com")]
        public void Constructor_WithInvalidEmailFormat_ThrowException(string invalidEmail)
        {
            
            //Act
            Action act=()=>new User(
                _testId,
                email:invalidEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Invalid email format*");
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public void Constructor_WithInvalidPhoneNumber_ThrowArgumentException(string invalidPhoneNumber)
        {
            //Act
            Action act = () => new User(
                 _testId,
                 _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                phoneNumber: invalidPhoneNumber,
                _validDateOfBirth,
                _validRole
                );

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Phone Number cannot be empty*");
        }
        [Theory]
        [InlineData("123")]
        [InlineData("abcd")]
        [InlineData("1234567890")] // Exactly 10 digits
        [InlineData("1234567890b")] // Exactly 10 digits and 1 alphabet
        [InlineData("112345678900")] // Exactly12 digits
        public void Constructor_WithShortPhoneNumber_ThrowArgumentException(string invalidPhoneNumber)
        {
            //Act
            Action act = () => new User(
                 _testId,
                 _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                phoneNumber: invalidPhoneNumber,
                _validDateOfBirth,
                _validRole
                );

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Phone number must be exactly 11 digits*");
        }

        [Fact]
        public void Constructor_WithValidPhoneNumberContainingNonDigits_ShouldCleanAndAccept()
        {
            // Arrange
            var phoneWithFormatting = "11223344556"; // Contains exactly 11 digits

            // Act
            var user = new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                phoneWithFormatting,
                _validDateOfBirth,
                _validRole);

            // Assert
            user.PhoneNumber.Should().Be(phoneWithFormatting.Trim());
        }

        [Theory]
        [InlineData("A")] // Too short
        [InlineData(" A")] // Only whitespace
        [InlineData(" A ")] // Only whitespace
        public void Constructor_WithShortLastName_ShouldThrowArgumentException(string shortLastName)
        {
            // Act
            Action act = () => new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                _validFirstName,
                shortLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*LastName must be at least 2 characters long*");
        }  
        [Theory]
        [InlineData("B")] // Too short
        [InlineData(" B")] // 1 char, one  whitespace
        [InlineData(" B ")]
        public void Constructor_WithShortFirstName_ShouldThrowArgumentException(string shortFirstName)
        {
            // Act
            Action act = () => new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                shortFirstName,
                _validLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            //Assert
            act.Should().Throw<ArgumentException>().WithMessage("*FirstName must be at least 2 characters long*");
        }

        [Fact]
        public void Constructor_WithUnderageUser_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var underageDateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18).AddDays(1));

            // Act
            Action act = () => new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                underageDateOfBirth,
                _validRole);

            //Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*User must be at least {User.MinimumAge} years old*");
        }
        [Fact]
        public void Constructor_WithExactlyMinimumAge_ShouldCreateUserSuccessfully()
        {
            // Arrange
            var minimumAgeDateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18));

            // Act
            var user = new User(
                _testId,
                _validEmail,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                minimumAgeDateOfBirth,
                _validRole);

            // Assert
            user.Should().NotBeNull();
            user.DateOfBirth.Should().Be(minimumAgeDateOfBirth);
        }
        [Fact]
        public void Constructor_ShouldTrimAndLowerCaseEmail()
        {
            // Arrange
            var emailWithSpacesAndCaps = "  Test@Example.COM  ";

            // Act
            var user = new User(
                _testId,
                emailWithSpacesAndCaps,
                _validPasswordHash,
                _validFirstName,
                _validLastName,
                _validPhoneNumber,
                _validDateOfBirth,
                _validRole);

            // Assert
            user.Email.Should().Be("test@example.com");
        }
    }
}

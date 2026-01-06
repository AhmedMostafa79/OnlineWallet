using FluentAssertions;
using Moq;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Application.Services;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Tests.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ICustomerService> _mockCustomerService;
        private readonly Mock<ITokenService> _mockTokenService;
        private readonly IAuthService _authService;
        public AuthServiceTests()
        {
            _mockCustomerService=new Mock<ICustomerService>();
            _mockTokenService = new Mock<ITokenService>();
            _authService = new AuthService(_mockCustomerService.Object, _mockTokenService.Object);
        }
        [Fact]
        public async Task RegisterAsync_WhenRegistrationSuccessful_ShouldReturnAuthenticationModel()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Email = "test@gmail.com",
                Password = "123345",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "01234567899",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            };

            var createdUser = new GetUserDto {
                 Id=Guid.NewGuid(),
                PhoneNumber= registerModel.PhoneNumber,
                Email= registerModel.Email,
                FirstName= registerModel.FirstName,
                LastName= registerModel.LastName,
                Role= UserRole.Customer,
                CurrentAccountNumber=Guid.NewGuid(),
                DateOfBirth= registerModel.DateOfBirth,
                };

            var tokenModel = new TokenResponseModel
            {
                Token = "jwt_token",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                TokenType = "Bearer"
            };

            _mockCustomerService.Setup(x => x.IsEmailRegistered(registerModel.Email)).ReturnsAsync(false);
            
            _mockCustomerService.Setup(x => x.CreateCustomerAsync(registerModel))
                .ReturnsAsync(createdUser);

            _mockTokenService.Setup(x => x.GetToken(createdUser))
                .Returns(tokenModel);

            // Act
            var result = await _authService.RegisterAsync(registerModel);

            // Assert
            result.Should().NotBeNull();
            result.IsAuthenticated.Should().BeTrue();
            result.Token.Should().Be(tokenModel.Token);
            result.Email.Should().Be(registerModel.Email);
            result.UserName.Should().Be("Test User");
            result.Role.Should().Be(UserRole.Customer);
        }
        [Fact]
        public async Task RegisterAsync_WhenEmailAlreadyRegistered_ShouldThrowException()
        {
            // Arrange
            var registerModel = new RegisterModel
          
            {
                Email = "test@gmail.com",
                Password = "123345",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "01234567899",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            };
            _mockCustomerService.Setup(x => x.IsEmailRegistered(registerModel.Email))
            .ReturnsAsync(true);

            // Act
            var act = async ()=> await _authService.RegisterAsync(registerModel);

            // Assert
           await  act.Should().ThrowAsync<InvalidOperationException>();

           _mockCustomerService.Verify(x=>x.IsEmailRegistered(registerModel.Email),Times.Once());
             
        }
        [Fact]
        public async Task RegisterAsync_WhenCustomerServiceFails_ShouldThrowException()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Email = "test@gmail.com",
                Password = "123345",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "01234567899",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            };

            var createdUser = new GetUserDto
            {
                Id = Guid.NewGuid(),
                PhoneNumber = registerModel.PhoneNumber,
                Email = registerModel.Email,
                FirstName = registerModel.FirstName,
                LastName = registerModel.LastName,
                Role = UserRole.Customer,
                CurrentAccountNumber = Guid.NewGuid(),
                DateOfBirth = registerModel.DateOfBirth,
            };

            var tokenModel = new TokenResponseModel
            {
                Token = "jwt_token",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                TokenType = "Bearer"
            };

            _mockCustomerService.Setup(x => x.IsEmailRegistered(registerModel.Email)).ReturnsAsync(false);

            _mockCustomerService.Setup(x => x.CreateCustomerAsync(registerModel))
                .ThrowsAsync(new Exception("Database error"));

            _mockTokenService.Setup(x => x.GetToken(createdUser))
                .Returns(tokenModel);

            // Act
            var act = async ()=> await _authService.RegisterAsync(registerModel);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage($"Failed to register customer with email {registerModel.Email}");
        }

        [Fact]
        public async Task LoginAsync_WhenCredentialsValid_ShouldReturnAuthenticationModel()
        {
            // Arrange
            var requestModel = new TokenRequestModel
            {
                Email="test@gmail.com",
                Password="12345"
            };

            var user = new GetUserDto
            {
                Id = Guid.NewGuid(),
                PhoneNumber = "01234567899",
                Email = requestModel.Email,
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Customer,
                CurrentAccountNumber = Guid.NewGuid(),
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            };

            _mockCustomerService.Setup(x => x.FindByEmailAsync(requestModel.Email))
                .ReturnsAsync(user);

            _mockCustomerService.Setup(x => x.CheckUserPasswordAsync(user.Id, requestModel.Password))
                .ReturnsAsync(true);

            var authToken = new TokenResponseModel
            {
                Token = "jwt_token",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                TokenType = "Bearer"
            };
            _mockTokenService.Setup(x => x.GetToken(user)).Returns(authToken);

            // Act
            var result= await _authService.LoginAsync(requestModel);
            
            // Assert 
            result.Should().NotBeNull();
            result.Email.Should().Be(requestModel.Email);
            result.IsAuthenticated.Should().BeTrue();
            result.Token.Should().Be(authToken.Token);
            result.UserName.Should().Be("Test User");
        }
        [Fact]
        public async Task LoginAsync_WhenUserNotFound_ShouldThrowException()
        {
            //Arrange
            var requestModel = new TokenRequestModel
            {
                Email = "test@gmail.com",
                Password = "12345"
            };
            _mockCustomerService.Setup(x => x.FindByEmailAsync(requestModel.Email))
                .ThrowsAsync(new KeyNotFoundException("user not found"));
            
            // Act
            var act=async ()=> await _authService.LoginAsync(requestModel);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
                
            
            _mockCustomerService.Verify(x => x.FindByEmailAsync(requestModel.Email), Times.Once);
            _mockCustomerService.Verify(x =>
            x.CheckUserPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>())
            , Times.Never);
                
        }

        [Fact]
        public async Task LoginAsync_WhenPasswordIncorrect_ShouldThrowException()
        {
            // Arrange
            var requestModel = new TokenRequestModel
            {
                Email = "test@gmail.com",
                Password = "wrongpassword"
            };

            var user = new GetUserDto
            {
                Id = Guid.NewGuid(),
                Email = requestModel.Email,
                FirstName = "Test",
                LastName = "User"
            };

            _mockCustomerService.Setup(x => x.FindByEmailAsync(requestModel.Email))
                .ReturnsAsync(user);

            _mockCustomerService.Setup(x => x.CheckUserPasswordAsync(user.Id, requestModel.Password))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _authService.LoginAsync(requestModel);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Invalid email or password");

            _mockCustomerService.Verify(x => x.FindByEmailAsync(requestModel.Email), Times.Once);
            _mockCustomerService.Verify(x => x.CheckUserPasswordAsync(user.Id, requestModel.Password), Times.Once);
        }
    }
}

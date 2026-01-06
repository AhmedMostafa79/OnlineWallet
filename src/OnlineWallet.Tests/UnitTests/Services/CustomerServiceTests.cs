using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Models;
using OnlineWallet.Application.DTOs;
using FluentAssertions;
using OnlineWallet.Domain.Enums;
using Castle.Core.Resource;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Data;
using Azure.Core;


namespace OnlineWallet.Tests.UnitTests.Services
{
    public class CustomerServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IHashService> _mockHashService;
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly ICustomerService _customerService;

        public CustomerServiceTests()
        {
            _mockUnitOfWork=new Mock<IUnitOfWork>();
            _mockHashService = new Mock<IHashService>();
            _mockAccountService = new Mock<IAccountService>();
            _mockAuditLogService = new Mock<IAuditLogService>();

            _customerService = new CustomerService(
                _mockUnitOfWork.Object,
                _mockAccountService.Object,
                _mockAuditLogService.Object,
                _mockHashService.Object
                );
        }
        [Fact]
        public async Task CreateCustomerAsync_WithValidData_ShouldCreateCustomerAndAccount()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Email = "test@example.com",
                PhoneNumber = "11234567890",//11 digits
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
                Password="password123!"
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            var expectedUserId = Guid.NewGuid();
            var expectedAccountNumber=Guid.NewGuid();
            var hashedPassword = "hashed_password_123";

            _mockUnitOfWork.Setup(x=>x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
            
            _mockHashService.Setup(x=>x.HashPassword(registerModel.Password))
                .Returns (hashedPassword);

            mockUsersRepo.Setup(x => x.AddAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

            _mockAccountService.Setup(x => x.CreateAccountAsync(
                It.IsAny<CreateAccountDto>(),
                false))
                .ReturnsAsync(new GetAccountDto
                (
                    accountNumber: expectedAccountNumber,
                    isActive: true,
                    ownerId: expectedUserId,
                    dateOpened: DateTime.UtcNow,
                    balance: 0
                    ));

            
            // Act
            var result = await _customerService.CreateCustomerAsync(registerModel);


            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(registerModel.Email.ToLower());
            result.FirstName.Should().Be(registerModel.FirstName);
            result.LastName.Should().Be(registerModel.LastName);
            result.PhoneNumber.Should().Be(registerModel.PhoneNumber);
            result.Role.Should().Be(UserRole.Customer);

            // Verify Interactions
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);

            mockUsersRepo.Verify(x=>x.AddAsync(It.IsAny<User>()),Times.Once);

            _mockAccountService.Verify(x => x.CreateAccountAsync(It.IsAny<CreateAccountDto>(), false), Times.Once);
            
            _mockAuditLogService.Verify(x => x.LogActionAsync(It.Is<AuditLog>(
                log => log.Status == AuditLogStatus.Success && log.ActionType == AuditLogActionType.CustomerCreation)), Times.Once);
        }
        
        [Fact]
        public async Task CreateCustomerAsync_WithUnderAgeCustomer_ShouldRollbackTransaction()
        {
            // Arrange
            var registerModel = new RegisterModel
            {
                Email = "test@example.com",
                PhoneNumber = "11234567890",//11 digits
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17)),
                Password = "password123!"
            };
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);

            // Act
            var act= async ()=> await _customerService.CreateCustomerAsync(registerModel);


            // Assert - Business Logic: Age Violation = Rollback
            await act.Should().ThrowAsync<InvalidOperationException>();

            // Verify transaction rolled back
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);

            // Verify Failure was logged
            _mockAuditLogService.Verify(x => x.LogActionAsync(It.Is<AuditLog>(
                log => log.Status == AuditLogStatus.Failed &&
                log.ActionType == AuditLogActionType.CustomerCreation &&
                log.PerformedBy == null &&
                log.Details.Contains(registerModel.Email))),
                Times.Once);

            // Verify No user was added (transaction rolled back)
            mockUsersRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);

            // Verify No account was created (transaction rolled back)
            _mockAccountService.Verify(x => x.CreateAccountAsync(It.IsAny<CreateAccountDto>(),It.IsAny<bool>()),
                Times.Never);

        }

        [Fact]
        public async Task IsEmailRegistered_WhenEmailExists_ShouldReturnTrue()
        {
            // Arrange
            var email = "test@gmail.com";
            var existingUser = new User
                (
                id: Guid.NewGuid(),
                email: email,
                passwordHash:"hashed_password",
                firstName: "John",
                lastName: "Doe",
                phoneNumber: "12345678900",
                role:UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                );

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(existingUser);

            // Act
            var result= await _customerService.IsEmailRegistered(email);

            // Assert
            result.Should().BeTrue();
            mockUsersRepo.Verify(x => x.FindByEmailAsync(email), Times.Once);
        }
        [Fact]
        public async Task IsEmailRegistered_WhenEmailNotExist_ShouldReturnFalse()
        {
            // Arrange
            var email = "test@gmail.com";
            
            var mockUsersRepo=new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((User)null);

            // Act
            var result = await _customerService.IsEmailRegistered(email);

            // Assert
            result.Should().BeFalse();
            mockUsersRepo.Verify(x => x.FindByEmailAsync(email), Times.Once);
        }

        [Fact]
        public async Task GetCustomerByIdAsync_WithExistingId_ShouldReturnCustomer()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var accountNumber = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-10);
            var existingCustomer = new User
                (
                id: customerId,
                email: "test@gmail.com",
                passwordHash: "hashed_password",
                firstName: "John",
                lastName: "Doe",
                phoneNumber: "12345678900",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                )
            {
                CurrentAccountNumber = accountNumber,
                CreatedAt = createdAt,
            };

            var expectedDto = new GetUserDto
            {
                Id = existingCustomer.Id,
                Email = existingCustomer.Email,
                FirstName = existingCustomer.FirstName,
                LastName = existingCustomer.LastName,
                PhoneNumber = existingCustomer.PhoneNumber,
                Role = existingCustomer.Role,
                DateOfBirth = existingCustomer.DateOfBirth,
                CurrentAccountNumber = existingCustomer.CurrentAccountNumber,
                DateCreated = existingCustomer.CreatedAt
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(existingCustomer);

            // Act
            var result=await _customerService.GetCustomerByIdAsync(customerId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            mockUsersRepo.Verify(x=>x.GetByIdAsync(customerId),Times.Once);
        }
        
        [Fact]
        public async Task GetCustomerByIdAsync_WithNonExistingId_ShouldThrowException()
        {
            // Arrange
            var customerId= Guid.NewGuid();

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync((User)null);

            // Act
            var act= async ()=> await _customerService.GetCustomerByIdAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
            mockUsersRepo.Verify(x => x.GetByIdAsync(customerId), Times.Once);

        }

        [Fact]
        public async Task FindCustomerByEmailAsync_WithCustomerExists_ShouldReturnCustomer()
        {
            // Arrange
            var accountNumber = Guid.NewGuid();
            var createdAt = DateTime.UtcNow.AddDays(-10);
            var email = "test@gmail.com";

            var existingCustomer = new User
               (
               id: Guid.NewGuid(),
               email: email,
               passwordHash: "hashed_password",
               firstName: "John",
               lastName: "Doe",
               phoneNumber: "12345678900",
               role: UserRole.Customer,
               dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
               )
            {
                CurrentAccountNumber = accountNumber,
                CreatedAt = createdAt
            };

            var expectedDto = new GetUserDto
            {
                Id = existingCustomer.Id,
                Email =existingCustomer.Email,
                FirstName = existingCustomer.FirstName,
                LastName = existingCustomer.LastName,
                PhoneNumber = existingCustomer.PhoneNumber,
                Role = existingCustomer.Role,
                DateOfBirth = existingCustomer.DateOfBirth,
                CurrentAccountNumber = existingCustomer.CurrentAccountNumber,
                DateCreated = existingCustomer.CreatedAt
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.FindByEmailAsync(email))
                .ReturnsAsync(existingCustomer);

            // Act
            var result = await _customerService.FindByEmailAsync(email);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);

            mockUsersRepo.Verify(x => x.FindByEmailAsync(email), Times.Once);
        }

        [Fact]
        public async Task FindByEmailAsync_WithCustomerNotExist_ShouldThrowException()
        {
            // Arrange
            var email = "invalid@gmail.com";

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.FindByEmailAsync(email)).ReturnsAsync((User)null);

            // Act
            var act = async () => await _customerService.FindByEmailAsync(email);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>()
                 .WithMessage($"Customer with email {email} not found.");
            mockUsersRepo.Verify(x => x.FindByEmailAsync(email), Times.Once);
        }

        [Fact]
        public async Task CheckUserPasswordAsync_WithMatchingPassword_ShouldReturnTrue()
        {

            // Arrange
            var customerId=Guid.NewGuid();
            var password = "plain_Password";
            var hashedPassword = "hashed_Password";

            var existingCustomer = new User
             (
             id: customerId,
             email: "test@gmail.com",
             passwordHash: hashedPassword,
             firstName: "John",
             lastName: "Doe",
             phoneNumber: "12345678900",
             role: UserRole.Customer,
             dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
             );

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x=>x.GetByIdAsync(customerId)).ReturnsAsync(existingCustomer);
            _mockHashService.Setup(x => x.VerifyPassword(password,hashedPassword)).Returns(true);

            // Act
            var result = await _customerService.CheckUserPasswordAsync(customerId, password);

            // Assert
            result.Should().BeTrue();
            _mockHashService.Verify(x => x.VerifyPassword(password, hashedPassword), Times.Once);
        }
        [Fact]
        public async Task CheckUserPasswordAsync_WithMismatchingPassword_ShouldReturnFalse()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var password = "invalid_password";
            var hashedPassword = "valid_hash";
            var existingCustomer = new User
           (
               id: customerId,
               email: "test@gmail.com",
               passwordHash: hashedPassword,
               firstName: "John",
               lastName: "Doe",
               phoneNumber: "12345678900",
               role: UserRole.Customer,
               dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
           );

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(existingCustomer);
            _mockHashService.Setup(x => x.VerifyPassword(password, hashedPassword)).Returns(false);

            // Act
            var result=await _customerService.CheckUserPasswordAsync(customerId, password);

            // Assert
            result.Should().BeFalse();
            _mockHashService.Verify(x => x.VerifyPassword(password, hashedPassword), Times.Once);
        }
        [Fact]
        public async Task CheckUserPasswordAsync_WithNonExistingCustomer_ShouldThrowException()
        {
            // Arrange
            var customerId=Guid.NewGuid();
            var password = "plain_Password";

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync((User)null);

            // Act
            var act =async ()=> await _customerService.CheckUserPasswordAsync(customerId, password);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
            _mockHashService.Verify(x =>
            x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);//checking function call after exception throw

        }
        [Fact]
        public async Task CheckUserPasswordAsync_WhenHashServiceThrows_ShouldWrapException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var existingCustomer = new User
            (
                id: customerId,
                email: "test@gmail.com",
                passwordHash: "hashed_Password",
                firstName: "John",
                lastName: "Doe",
                phoneNumber: "12345678900",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
            );

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId))
            .ReturnsAsync(existingCustomer);

            _mockHashService.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
        .Throws(new Exception("Hash service failed"));
            // Act
            var act = async () => await _customerService.CheckUserPasswordAsync(customerId, existingCustomer.PasswordHash);

            // Assert
            await act.Should().ThrowAsync<Exception>();

        }

        [Fact]
        public async Task GetAllCustomersAsync_ShouldReturnAllCustomers()
        {
            // Arrange
            var customers = new List<User>
            {
               new User(Guid.NewGuid(), "user1@example.com", "hash1", "John", "Doe",
            "11234567890", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)), UserRole.Customer)
               {
                   CreatedAt=DateTime.UtcNow,
                   CurrentAccountNumber=Guid.NewGuid()
               },

                new User(Guid.NewGuid(), "user2@example.com", "hash2", "Jane", "Smith",
            "00123456789", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)), UserRole.Admin)
               {
                   CreatedAt=DateTime.UtcNow,
                   CurrentAccountNumber=Guid.NewGuid()
               }
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetAllCustomersAsync()).ReturnsAsync(customers);

            // Act
            var result= await _customerService.GetAllCustomersAsync();

            // Assert
            // Assert number of customers
            result.Should().HaveCount(customers.Count);

            result.Should().BeEquivalentTo(customers
                .Select(customer => new GetUserDto
                {
                    Id = customer.Id,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    Email = customer.Email,
                    Role = customer.Role,
                    CurrentAccountNumber = customer.CurrentAccountNumber,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    DateCreated = customer.CreatedAt
                }));
            mockUsersRepo.Verify(x=>x.GetAllCustomersAsync(), Times.Once());
            }
        [Fact]
        public async Task GetCurrentAccountAsync_WithExistingAccount_ShouldReturnAccount()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var accountNumber = Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
       "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = accountNumber
            };

          
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

           
            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            var expectedAccount = new GetAccountDto(accountNumber, true, customerId, 1000m, DateTime.UtcNow);
            _mockAccountService.Setup(x => x.GetAccountByIdAsync(accountNumber)).ReturnsAsync(expectedAccount);


            // Act
            var result=await _customerService.GetCurrentAccountAsync(customerId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedAccount);
            _mockAccountService.Verify(x => x.GetAccountByIdAsync(accountNumber), Times.Once);

        }
        [Fact]
        public async Task GetCurrentAccountAsync_WithNonBusinessException_ShouldWrapIt()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var accountNumber = Guid.NewGuid();

            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
                "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = accountNumber
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            // Non-business exception (e.g., database connection failure)
            _mockAccountService.Setup(x => x.GetAccountByIdAsync(accountNumber))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Assuming ExceptionHelper.IsBusinessException returns false for generic Exception

            // Act
            Func<Task> act = async () => await _customerService.GetCurrentAccountAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<Exception>(); // Should be wrapped
                
        }

        [Fact]
        public async Task GetAccountBalance_ShouldReturnAccountBalance()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            decimal balance = 1000m;
            var accountNumber= Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
                "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = accountNumber
            };
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x=>x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            _mockAccountService.Setup(x => x.GetAccountBalanceAsync(accountNumber)).ReturnsAsync(balance);
             
            // Act
            var result=await _customerService.GetCurrentAccountBalanceAsync(customerId);

            // Assert
            result.Should().Be(balance);
            _mockAccountService.Verify(x=>x.GetAccountBalanceAsync(accountNumber),Times.Once());
            _mockUnitOfWork.Verify(x => x.UsersRepository, Times.Once);
        }
        [Fact] 
        public async Task GetAccountBalance_WithError_ShouldThrowException()
        {
            // Arrange
            var customerId= Guid.NewGuid();
            var accountNumber= Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
               "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = accountNumber
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            _mockAccountService.Setup(x => x.GetAccountBalanceAsync(accountNumber)).ThrowsAsync(new Exception());

            // Act
            var act=async()=> await _customerService.GetCurrentAccountBalanceAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
        [Fact]
        public async Task GetTransactionHistory_WithValidCustomer_ShouldReturnHistory()
        {
            // Arrange
            var customerId=Guid.NewGuid();
            var expectedHistory = new List<AuditLog>
            {
                new AuditLog(
                    Guid.NewGuid(),
                    AuditLogActionType.Transfer,
                    DateTime.UtcNow.AddHours(-1),
                    AuditLogStatus.Success,
                    performedBy: customerId,
                    details: "Transfer completed"),
                new AuditLog(
                    Guid.NewGuid(),
                    AuditLogActionType.Deposit,
                    DateTime.UtcNow.AddHours(-2),
                    AuditLogStatus.Success,
                    performedBy: customerId,
                    details: "Deposit completed")
            };

            _mockAuditLogService.Setup(x=>x.GetTransactionHistoryAsync(customerId)).ReturnsAsync(expectedHistory);

            // Act
            var result = await _customerService.GetTransactionHistoryAsync(customerId);

            // Assert
            result.Should().BeEquivalentTo(expectedHistory);
            result.Count().Should().Be(expectedHistory.Count);
        }
        [Fact]
        public async Task GetTransactionHistory_WithError_ShouldReturnException()
            {
                // Arrange
                var customerId=Guid.NewGuid();
                _mockAuditLogService.Setup(x => x.GetTransactionHistoryAsync(customerId)).ThrowsAsync(new Exception());

                // Act
                var act = async()=> await _customerService.GetTransactionHistoryAsync(customerId);

                // Assert
                await act.Should().ThrowAsync<Exception>();
            }

        [Fact]
        public async Task UpdateCustomerAsync_WithValidData_ShouldUpdateCustomer()
        {
            // Arrange
            var customerId=Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
              "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = Guid.NewGuid()
            };

            var updateDto = new UpdateUserDto
            {
                FirstName = "Test",
                LastName = "User"
            };

            var mockUsersRepo = new Mock<IUsersRepository> ();
            _mockUnitOfWork.Setup(x=>x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            mockUsersRepo.Setup(x=>x.GetByIdAsync(customerId)).ReturnsAsync(customer);

           // Act
           await _customerService.UpdateCustomerAsync(customerId, updateDto);

            //Assert
            customer.FirstName.Should().Be(updateDto.FirstName);
            customer.LastName.Should().Be(updateDto.LastName);
            mockUsersRepo.Verify(x=>x.GetByIdAsync(customerId),Times.Once);
            mockUsersRepo.Verify(x=>x.Update(It.IsAny<User>()),Times.Once);
            _mockUnitOfWork.Verify(x=>x.SaveChangesAsync(),Times.Once);
        }
        [Fact]
        public async Task UpdateCustomerAsync_WithEmptyOrNullNames_ShouldNotUpdateThoseUpdates()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
              "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = Guid.NewGuid()
            };

            var updateDto = new UpdateUserDto
            {
                FirstName = null,
                LastName = ""
            };
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);
            // Act
         await _customerService.UpdateCustomerAsync(customerId, updateDto);

            // Assert
            customer.FirstName.Should().Be("John");
            customer.LastName.Should().Be("Doe");
            mockUsersRepo.Verify(x => x.GetByIdAsync(customerId), Times.Once);
            mockUsersRepo.Verify(x => x.Update(It.IsAny<User>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);

        }
        [Fact]
        public async Task TransferAsync_WithValidData_ShouldTransferAmount()
        {
            // Arrange
            var customerId= Guid.NewGuid();
            var toAccount = Guid.NewGuid();
            var amount = 1000m;

            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
             "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = Guid.NewGuid()
            };

            TransferRequestModel request = new TransferRequestModel
            {
                ToAccount = toAccount,
                Amount = amount
            };
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            _mockAccountService.Setup(x => x.TransferAsync(customer.CurrentAccountNumber, request.ToAccount,request.Amount))
                .Returns(Task.CompletedTask);
           
            // Act
            await _customerService.TransferAsync(customerId, request);


            // Assert
            _mockAccountService.Verify(x=>x.TransferAsync(customer.CurrentAccountNumber,toAccount, amount), 
                Times.Once);
        }

        [Fact]
        public async Task TransferAsync_WithAccountServiceError_ShouldThrowException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var toAccount = Guid.NewGuid();
            var amount = -1000m;//amount is negative to be caught by ArgumentException

            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
             "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = Guid.NewGuid()
            };

            TransferRequestModel request = new TransferRequestModel
            {
                ToAccount =toAccount,
                Amount = amount
            };
            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            _mockAccountService.Setup(x=>x.TransferAsync(customer.CurrentAccountNumber,toAccount,amount))
                .ThrowsAsync(new ArgumentException());// as amount is negative

            // Act
           var act= async() => await _customerService.TransferAsync(customerId, request);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
            _mockAccountService.Verify(x => x.TransferAsync(customer.CurrentAccountNumber, toAccount, amount)
            ,Times.Once);
        }

        [Fact]
        public async Task DeleteCustomerAsync_WithValidCustomer_ShouldDeleteCustomer()
        {

            // Arrange
            var customerId = Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
            "00123456789", DateOnly.MinValue, UserRole.Customer)
            {
                CurrentAccountNumber = Guid.NewGuid()
            };

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            
            _mockUnitOfWork.Setup(x=>x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x=>x.CommitTransactionAsync()).Returns(Task.CompletedTask);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            var customerAccounts = new List<GetAccountDto>
            {
                new GetAccountDto(Guid.NewGuid(),true,customerId,0,DateTime.UtcNow),
                new GetAccountDto(Guid.NewGuid(),true,customerId,0,DateTime.UtcNow)

            };
            _mockAccountService.Setup(x => x.GetAccountByCustomerIdAsync(customerId))
                .ReturnsAsync(customerAccounts);
            
            // Act
           await _customerService.DeleteCustomerAsync(customerId);

            // Assert
             mockUsersRepo.Verify(x => x.Delete(customer), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Never);

            //Verify Accounts were deleted
            _mockAccountService.Verify(x => x.DeleteAccountAsync(customerAccounts[0].AccountNumber));
            _mockAccountService.Verify(x => x.DeleteAccountAsync(customerAccounts[1].AccountNumber));

        }

        [Fact]
        public async Task DeleteCustomerAsync_WhereHasBalance_ShouldRollBackAndThrow()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
       "12345678900", DateOnly.MinValue, UserRole.Customer);

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);

            _mockUnitOfWork.Setup(x=>x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            var customerAccounts = new List<GetAccountDto>
            {
                new GetAccountDto(Guid.NewGuid(),true,customerId,0,DateTime.UtcNow),
                new GetAccountDto(Guid.NewGuid(),true,customerId,500m,DateTime.UtcNow)
            };
            _mockAccountService.Setup(x => x.GetAccountByCustomerIdAsync(customerId)).ReturnsAsync(customerAccounts);
           

            // Act
            var act = async () => await _customerService.DeleteCustomerAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
            mockUsersRepo.Verify(x => x.Delete(customer), Times.Never);

        }

        [Fact]
        public async Task DeleteCustomerAsync_WhenCustomerNotFound_ShouldRollbackAndThrow()
        {
            // Arrange
            var customerId = Guid.NewGuid();

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId))
                .ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _customerService.DeleteCustomerAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteCustomerAsync_WhenAccountServiceFails_ShouldRollbackAndWrapException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var customer = new User(customerId, "test@example.com", "hash", "John", "Doe",
                "12345678900", DateOnly.MinValue, UserRole.Customer);

            var mockUsersRepo = new Mock<IUsersRepository>();
            _mockUnitOfWork.Setup(x => x.UsersRepository).Returns(mockUsersRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(IsolationLevel.ReadCommitted)).Returns(Task.CompletedTask);

            mockUsersRepo.Setup(x => x.GetByIdAsync(customerId)).ReturnsAsync(customer);

            _mockAccountService.Setup(x => x.GetAccountByCustomerIdAsync(customerId))
                .ReturnsAsync(new List<GetAccountDto>
                {
            new GetAccountDto(Guid.NewGuid(), true, customerId,0, DateTime.UtcNow)
                });

            // Account deletion fails
            _mockAccountService.Setup(x => x.DeleteAccountAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Account deletion failed"));

            // Act
            Func<Task> act = async () => await _customerService.DeleteCustomerAsync(customerId);

            // Assert
            await act.Should().ThrowAsync<Exception>();

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Never);
        }
    }
}

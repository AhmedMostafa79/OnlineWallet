using FluentAssertions;
using Moq;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Models;
using OnlineWallet.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Tests.UnitTests.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly IAccountService _accountService;
        private readonly Mock<IAccountsRepository> _mockAccountsRepo;
        public AccountServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockAccountsRepo = new Mock<IAccountsRepository>();
            _mockUnitOfWork.Setup(x => x.AccountsRepository).Returns(_mockAccountsRepo.Object);
            _accountService = new AccountService(_mockUnitOfWork.Object, _mockAuditLogService.Object);
        }
        [Fact]
        public async Task CreateAccountAsync_WithValidData_ShouldCreateAccount()
        {
            // Arrange
            var customerId= Guid.NewGuid();
            var createAccountDto = new CreateAccountDto
            {
                OwnerId = customerId,
                CreatedAt= DateTime.UtcNow
            
            };
            var accountDto = new GetAccountDto
            (
                 accountNumber: Guid.NewGuid(),
                       isActive: true,
                       ownerId: customerId,
                       balance:0,
                       dateOpened: createAccountDto.CreatedAt
            );

            _mockUnitOfWork.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
            // Act
            var result = await _accountService.CreateAccountAsync(createAccountDto, true);

            // Assert
            result.OwnerId.Should().Be(customerId);
            result.DateOpened.Should().Be(createAccountDto.CreatedAt);
        }

        [Fact]
        public async Task CreateAccountAsync_WithInternalError_ShouldThrowException()
        {
            // Arrange
            var customerId = Guid.NewGuid();
            var createAccountDto = new CreateAccountDto
            {
                OwnerId = customerId,
                CreatedAt = DateTime.UtcNow
            };

            var accountDto = new GetAccountDto
            (
                 accountNumber: Guid.NewGuid(),
                       isActive: true,
                       ownerId: customerId,
                       balance: 0,
                       dateOpened: DateTime.UtcNow
            );

            _mockAccountsRepo.Setup(x => x.AddAsync(It.IsAny<Account>())).Throws(new Exception("Internal Server Error"));


            // Act
            var act =async ()=>await _accountService.CreateAccountAsync(createAccountDto, true);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockUnitOfWork.Verify(x=>x.SaveChangesAsync(), Times.Never());
        }
        [Fact]
        public async Task GetAccountByIdAsync_WithExistingAccount_ShouldGetAccount()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), true, Guid.NewGuid(), DateTime.UtcNow);

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(account.AccountNumber))
      .ReturnsAsync(account);

            // Act
            var result = await _accountService.GetAccountByIdAsync(account.AccountNumber);

            // Assert
            result.Should().NotBeNull();
            result.AccountNumber.Should().Be(account.AccountNumber);
            result.IsActive.Should().BeTrue();
            result.OwnerId.Should().Be(account.OwnerId);
            result.Balance.Should().Be(account.Balance);
            result.DateOpened.Should().Be(account.CreatedAt);
        }
        [Fact]
        public async Task GetAccountByIdAsync_WithNonExistentAccount_ShouldThrowException()
        {
            // Arrange
            var nonExistentAccountNumber = Guid.NewGuid();

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(nonExistentAccountNumber))
                .ReturnsAsync((Account)null);

            // Act
            Func<Task> act = async () => await _accountService.GetAccountByIdAsync(nonExistentAccountNumber);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }
        [Fact]
        public async Task GetAccountBalance_WithExistingAccount_ShouldReturnBalance()
        {
            // Arrange
            var accountNumber=Guid.NewGuid();
            var balance = 1000m;
            var account= new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);
            account.Deposit(balance);

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(accountNumber)).ReturnsAsync(account);

            // Act
            var result = await _accountService.GetAccountBalanceAsync(accountNumber);

            // Assert
            result.Should().Be(balance);
            _mockAccountsRepo.Verify(x => x.GetByIdAsync(accountNumber), Times.Once);

        }

        [Fact] 
        public async Task GetAllAccountsAsync_ShouldReturnAllAccounts()
        {
            // Arrange
            var accounts = new List<Account>
            {
            new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow),
            new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow),
            new Account(Guid.NewGuid(),false,Guid.NewGuid(),DateTime.UtcNow)
            };
            accounts[0].Deposit(1000m);
            accounts[1].Deposit(500m);
            _mockAccountsRepo.Setup(x => x.GetAllAsync()).ReturnsAsync(accounts);
           
            // Act
            var result=await _accountService.GetAllAccountsAsync();

            // Assert
            result.Count().Should().Be(accounts.Count());
            result.Should().BeEquivalentTo(
                accounts.Select(
                a=>new GetAccountDto(
                    accountNumber: a.AccountNumber,
                    isActive: a.IsActive,
                    ownerId: a.OwnerId,
                    balance: a.Balance,
                    dateOpened: a.CreatedAt
                    ))
                );
        }
        [Fact]
        public async Task GetAccountByCustomerIdAsync_WithExistingUser_ShouldReturnUserAccounts()
        {
            // Arrange
            var customerId= Guid.NewGuid();
            var customerAccounts = new List<Account>
            {
            new Account(Guid.NewGuid(),true,customerId,DateTime.UtcNow),
            new Account(Guid.NewGuid(),false,customerId,DateTime.UtcNow)
            };
            
            _mockAccountsRepo.Setup(x => x.GetByCustomerIdAsync(customerId))
                .ReturnsAsync(customerAccounts);

            // Act
            var result=await _accountService.GetAccountByCustomerIdAsync(customerId);

            // Assert
            result.Count().Should().Be(customerAccounts.Count());
            result.Should().BeEquivalentTo(
                customerAccounts.Select(
                    a => new GetAccountDto(
                         accountNumber: a.AccountNumber,
                    isActive: a.IsActive,
                    ownerId: a.OwnerId,
                    balance: a.Balance,
                    dateOpened: a.CreatedAt

                        )
                ));
        }

        [Fact]
        public async Task ActivateAccountAsync_WhenInActive_ShouldActivateAccount()
        {
            // Arrange
            var customerAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: false,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);

            var accounts = new List<Account>
            {
                customerAccount,
            new Account(Guid.NewGuid(),false,Guid.NewGuid(),DateTime.UtcNow)
            };

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(customerAccount.AccountNumber)).
                ReturnsAsync(customerAccount);

            // Act
             await _accountService.ActivateAccountAsync(customerAccount.AccountNumber);

            // Assert

            customerAccount.IsActive.Should().BeTrue();
            //_mockAccountsRepo.Verify(x => x.GetByIdAsync(customerAccount.AccountNumber), Times.Once);
            //_mockAccountsRepo.Verify(x => x.Update(It.IsAny<Account>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ActivateAccountAsync_WhenAlreadyActive_ShouldThrowException()
        {
            // Arrange
            var customerAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);

            var accounts = new List<Account>
            {
                customerAccount,
            new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow)
            };

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(customerAccount.AccountNumber)).
                ReturnsAsync(customerAccount);

            // Act
            var act = async () => await _accountService.ActivateAccountAsync(customerAccount.AccountNumber);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            _mockAccountsRepo.Verify(x => x.GetByIdAsync(customerAccount.AccountNumber), Times.Once);
            _mockAccountsRepo.Verify(x => x.Update(It.IsAny<Account>()), Times.Never);
        }
        [Fact]
        public async Task TransferAsync_WithValidData_ShouldTransferAmount()
        {
            // Arrange
            var fromAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);
            fromAccount.Deposit(600);//fromAccount initial balance

            var toAccount = new Account(Guid.NewGuid(), true, Guid.NewGuid(), DateTime.UtcNow);
            var initialBalance = 400;
            toAccount.Deposit(initialBalance);;//toAccount initial balance

            var amount = 500m;
            var accounts = new List<Account>
             {
               
                fromAccount,
                new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow),//another account in database
                toAccount
             };

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(fromAccount.AccountNumber)).ReturnsAsync(fromAccount);
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccount.AccountNumber)).ReturnsAsync(toAccount);

            _mockAuditLogService.Setup(x => x.LogTransferAsync(
                fromAccount,
                    toAccount.AccountNumber,
                    amount,
                    true)).Returns(Task.CompletedTask);
            // Act
            var result = _accountService.TransferAsync(fromAccount.AccountNumber, toAccount.AccountNumber, amount);

            // Assert
            fromAccount.Balance.Should().Be(600 - amount);
            toAccount.Balance.Should().Be(initialBalance+amount);

            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
            //verify that the transfer logging is done successfully
            _mockAuditLogService.Verify(
                x => x.LogTransferAsync(
                fromAccount,
                   toAccount.AccountNumber,
                     amount,
                    true)
                ,Times.Once);

        } 

        [Fact]
        public async Task TransferAsync_WithNonExistentDestination_ShouldThrowException()
        {
            // Arrange
            var fromAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow); ;
            
            var fromInitialBalance = 600;
            fromAccount.Deposit(fromInitialBalance);//fromAccount initial balance

            var toAccountNumber = Guid.NewGuid();

            var amount = 500m;
            var accounts = new List<Account>
             {
               
                fromAccount,
                new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow),//another account in database
             };

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(fromAccount.AccountNumber)).ReturnsAsync(fromAccount);
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccountNumber)).ReturnsAsync((Account)null);

            // Act
            var act=async () =>await  _accountService.TransferAsync(fromAccount.AccountNumber, toAccountNumber, amount);

            // Assert
            await act.Should().ThrowAsync< KeyNotFoundException>();

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);

            fromAccount.Balance.Should().Be(fromInitialBalance);//verify that initial balance didn't change
            _mockAccountsRepo.Verify(x => x.Update(fromAccount), Times.Never);

            //verify that the transfer logging is done successfully
            _mockAuditLogService.Verify(
                x => x.LogTransferAsync(
                fromAccount,
                   toAccountNumber,
                     amount,
                     false)
                , Times.Once);

        } 
        [Fact]
        public async Task TransferAsync_WithInactiveDestination_ShouldThrowException()
        {
            // Arrange
            var fromAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow); ;
            var fromInitialBalance = 600;
            fromAccount.Deposit(fromInitialBalance);//fromAccount initial balance

            var toAccount = new Account(Guid.NewGuid(), false, Guid.NewGuid(), DateTime.UtcNow);//inactive destination account
            var toInitialBalance = 400;
            toAccount.Deposit(toInitialBalance);;//toAccount initial balance

            var amount = 500m;
            var accounts = new List<Account>
             {
               
                fromAccount,
                new Account(Guid.NewGuid(),true,Guid.NewGuid(),DateTime.UtcNow),//another account in database
                toAccount
             };

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(fromAccount.AccountNumber)).ReturnsAsync(fromAccount);
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccount.AccountNumber)).ReturnsAsync(toAccount);

            // Act
            var act=async () =>await  _accountService.TransferAsync(fromAccount.AccountNumber, toAccount.AccountNumber, amount);

            // Assert
            await act.Should().ThrowAsync< InvalidOperationException>();

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);

            fromAccount.Balance.Should().Be(fromInitialBalance);//verify that initial balance didn't change
            toAccount.Balance.Should().Be(toInitialBalance);//verify that initial balance didn't change

            //verify that the transfer logging is done successfully
            _mockAuditLogService.Verify(
                x => x.LogTransferAsync(
                fromAccount,
                   toAccount.AccountNumber,
                     amount,
                     false)
                , Times.Once);

        }

        [Fact]
        public async Task DepositAsync_WithValidData_ShouldDepositAmount()
        {
            // Arrange

            var toAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);//inactive destination account
            var toInitialBalance = 400;
            toAccount.Deposit(toInitialBalance); ;//toAccount initial balance

            var amount = 500m;
            var accounts = new List<Account>
            {
                new Account(Guid.NewGuid(), true, Guid.NewGuid(), DateTime.UtcNow),
                toAccount
            };
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccount.AccountNumber)).ReturnsAsync(toAccount);
            var initiatedBy = Guid.NewGuid();
            // Act
            await _accountService.DepositAsync(initiatedBy, toAccount.AccountNumber,amount);

            // Assert
            toAccount.Balance.Should().Be(toInitialBalance + amount);
            _mockAuditLogService.Verify(
                x => x.LogDepositAsync(
                    initiatedBy,
                   toAccount.AccountNumber,
                    amount,
                    true)
            ,Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
        }
        [Fact]
        public async Task DepositAsync_WithInvalidAmount_ShouldThrowException()
        {
            // Arrange

            var toAccount = new Account(
                accountNumber: Guid.NewGuid(),
                isActive: true,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);//inactive destination account
            var toInitialBalance = 500;
            toAccount.Deposit(toInitialBalance); ;//toAccount initial balance

            var amount = -500m;
            var accounts = new List<Account>
            {
                new Account(Guid.NewGuid(), true, Guid.NewGuid(), DateTime.UtcNow),
                toAccount
            };
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccount.AccountNumber)).ReturnsAsync(toAccount);
            var initiatedBy = Guid.NewGuid();
            
            // Act
            var act=async ()=> await _accountService.DepositAsync(initiatedBy, toAccount.AccountNumber,amount);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();

            toAccount.Balance.Should().Be(toInitialBalance);

            //verify that the exception is thrown before transaction block at all
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(IsolationLevel.Serializable), Times.Never);
        }
        [Fact]
        public async Task DepositAsync_WithInactiveDestination_ShouldThrowException()
        {
            // Arrange
            var toAccount = new Account(
                accountNumber:Guid.NewGuid(), 
                isActive:false, 
                ownerId:Guid.NewGuid(), 
                createdAt:DateTime.UtcNow);//inactive destination account
            var toInitialBalance = 500;
            toAccount.Deposit(toInitialBalance); ;//toAccount initial balance

            var amount = 400m;
            var accounts = new List<Account>
            {
                toAccount,
                new Account(Guid.NewGuid(), true, Guid.NewGuid(), DateTime.UtcNow)
            };
            _mockAccountsRepo.Setup(x => x.GetByIdAsync(toAccount.AccountNumber)).ReturnsAsync(toAccount);
            var initiatedBy = Guid.NewGuid();
            
            // Act
            var act=async ()=> await _accountService.DepositAsync(initiatedBy, toAccount.AccountNumber,amount);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            toAccount.Balance.Should().Be(toInitialBalance );

            _mockAuditLogService.Verify(x => x.LogDepositAsync(
                    initiatedBy,
                   toAccount.AccountNumber,
                    amount,
                    false)
            ,Times.Once);

            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(), Times.Once);
        }
        [Fact]
        public async Task DeleteAccountAsync_WhenInactiveAndNoBalance_ShouldDeleteAccount()
        {
            // Arrange
            var accountNumber = Guid.NewGuid();
            //balance = 0 initially 
            var account = new Account(
                accountNumber: accountNumber,
                isActive: false,
                ownerId: Guid.NewGuid(),
                createdAt: DateTime.UtcNow);

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(accountNumber)).ReturnsAsync(account);

            // Act
            await _accountService.DeleteAccountAsync(accountNumber);

            // Assert
            _mockAccountsRepo.Verify(x => x.GetByIdAsync(accountNumber),Times.Once);
            _mockAccountsRepo.Verify(x => x.Delete(account),Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);

        }
        [Fact]
        public async Task DeleteAccountAsync_WhenHasBalance_ShouldThrowException()
        {
            // Arrange
            var accountNumber = Guid.NewGuid();
            //balance = 0 initially 
            var account = new Account(
               accountNumber: accountNumber,
               isActive: false, //inactive
               ownerId: Guid.NewGuid(),
               createdAt: DateTime.UtcNow);
            account.Deposit(0.01m);//add initial balance

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(accountNumber)).ReturnsAsync(account);

            // Act
            var act=async ()=> await _accountService.DeleteAccountAsync(accountNumber);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot delete account with balance*");
            _mockAccountsRepo.Verify(x => x.GetByIdAsync(accountNumber),Times.Once);

            _mockAccountsRepo.Verify(x => x.Delete(account),Times.Never);


        }
        [Fact]
        public async Task DeleteAccountAsync_WithActiveAccount_ShouldThrowException()
        {
            // Arrange
            var accountNumber = Guid.NewGuid();
            //balance = 0 initially 
            var account = new Account(
                accountNumber:accountNumber, 
                isActive:true,
                ownerId: Guid.NewGuid(), 
                createdAt:DateTime.UtcNow);

            _mockAccountsRepo.Setup(x => x.GetByIdAsync(accountNumber)).ReturnsAsync(account);

            // Act
            var act = async () => await _accountService.DeleteAccountAsync(accountNumber);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Cannot delete active account*");

            // Verify
            _mockAccountsRepo.Verify(x => x.GetByIdAsync(accountNumber), Times.Once);

            _mockAccountsRepo.Verify(x => x.Delete(account), Times.Never);

        }

    }
}

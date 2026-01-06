using FluentAssertions;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Tests.UnitTests.Models
{
    public class AccountTests
    {
        private readonly Guid _testAccountNumber = Guid.NewGuid();
        private readonly Guid _testOwnerId=Guid.NewGuid();
        private readonly DateTime _testCreatedAt= DateTime.UtcNow.AddDays(-1);
        
       //functionName_Criteria_expectedResult
        [Fact]
        public void Constructor_WithValidParameters_CreatesAccountSuccessfully()
        {
            //Arrange
            var isActive = true;

            //Act
            var account = new Account(
                accountNumber: _testAccountNumber,
                ownerId: _testOwnerId,
                createdAt: _testCreatedAt,
                isActive: isActive
                );

            //Assert
            account.Should().NotBeNull();
            account.AccountNumber.Should().Be( _testAccountNumber );
            account.OwnerId.Should().Be( _testOwnerId );
            account.IsActive.Should().Be(isActive);
            account.Balance.Should().Be(0);
            account.CreatedAt.Should().Be(_testCreatedAt);
        }
        [Theory]
        [InlineData(500.00)]
        [InlineData(0.50)]
        [InlineData(500.50)]
        public void Deposit_WithValidAmount_ShouldIncreaseBalance(decimal amount)
        {
            //Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);
            //Act
            account.Deposit(amount);
            //assert
            account.Balance.Should().Be(amount);

        }

        [Theory]
        [InlineData(-100)]
        [InlineData(-0.05)]
        [InlineData(0.0)]
        public void Deposit_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
        {
            //Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);
            
            //Act
            var act=()=>account.Deposit(invalidAmount);

            //Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"*Amount {invalidAmount} must be positive!*");
        }

        [Theory]
        [InlineData(100)]
        [InlineData(0.50)]
        [InlineData(999.99)]
        [InlineData(1000)]
        public void Withdraw_WithValidAmount_ShouldDecreaseBalance(decimal amount)
        {
            //Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);
            decimal initialBalance = 1000;
            account.Deposit(initialBalance);

            //Act
            account.Withdraw(amount);

            //Assert
            account.Balance.Should().Be(initialBalance - amount);
        }
        [Theory]
        [InlineData(500)]
        [InlineData(1000)]
        public void Withdraw_WithInvalidAmount_ShouldThrowInvalidOperationException(decimal invalidAmount)
        {
            //Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);
            decimal initialBalance = 499.99m;
            account.Deposit(initialBalance);
            //Act
            var act = () => account.Withdraw(invalidAmount);

            //Assert
            act.Should().Throw<InvalidOperationException>().WithMessage("*Insufficient funds.*");

        }
        [Theory]
        [InlineData(-100)]
        [InlineData(-0.05)]
        [InlineData(0.0)]
        public void Withdraw_WithInvalidAmount_ShouldThrowArgumentException(decimal invalidAmount)
        {
            //Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);
            var initialBalance = 100;
            account.Deposit(initialBalance);
            //Act
            var act = () => account.Withdraw(invalidAmount);

            //Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage($"*Amount {invalidAmount} must be positive!*");
        }
        [Fact]
        public void MultipleDepositsAndWithdrawals_ShouldCalculateCorrectFinalBalance()
        {
            // Arrange
            var account = new Account(accountNumber: _testAccountNumber, ownerId: _testOwnerId, createdAt: _testCreatedAt, isActive: true);

            // Act
            account.Deposit(1000);
            account.Withdraw(750);
            account.Deposit(100);
            account.Withdraw(50);
            account.Deposit(500);

            // Assert
            account.Balance.Should().Be(1000 - 750 +100- 50 + 500);
        }
    }
}

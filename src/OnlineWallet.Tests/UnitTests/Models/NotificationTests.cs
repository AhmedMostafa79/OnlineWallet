using FluentAssertions;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;

namespace OnlineWallet.Tests.UnitTests.Models
{
    public class NotificationTests
    {
        private readonly Guid _testId = Guid.NewGuid();
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly DateTime _testActionTime = DateTime.UtcNow;

        [Fact]
        public void Constructor_WithValidParameters_ShouldSetPropertiesCorrectly()
        {
            //Arrange
            decimal amount = 500.0m;
            string message = $"Deposited amount {amount} Successfully to you account";
            string title = "Deposit";
            var type = NotificationType.Deposit;

            //Act
            var notification = new Notification(
                id: _testId,
                userId: _testUserId,
                message: message,
                actionTime: _testActionTime,
                type: type,
                title: title
                );

            //Assert
            notification.Should().NotBeNull();
            notification.Id.Should().Be(_testId);
            notification.UserId.Should().Be(_testUserId);
            notification.Message.Should().Be(message);
            notification.ActionTime.Should().Be(_testActionTime);
            notification.Type.Should().Be(type);
            notification.Title.Should().Be(title);
        }
    }
}

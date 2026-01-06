using FluentAssertions;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Tests.UnitTests.Models
{
    public class AuditLogTests
    {
        private readonly Guid _testId=Guid.NewGuid();
        private readonly DateTime _testCreatedAt=DateTime.Now;
        [Fact]
        public void Constructor_WithValidParameters_ShouldSetPropertiesCorrectly()
        {
            //Arrange
            var auditLogActionType = AuditLogActionType.CustomerCreation;
            var auditLogStatus = AuditLogStatus.Success;
            var performedBy = Guid.NewGuid();
            string details = "Test details";

            //Act
            var auditLog = new AuditLog(
                id: _testId,
                actionType: auditLogActionType,
                createdAt: _testCreatedAt,
                status: auditLogStatus,
                performedBy: performedBy,
                details:details
                );

            //Assert
            auditLog.Should().NotBeNull();
            auditLog.Id.Should().Be(_testId);
            auditLog.CreatedAt.Should().Be(_testCreatedAt);
            auditLog.ActionType.Should().Be( auditLogActionType );
            auditLog.PerformedBy.Should().Be( performedBy );
            auditLog.Details.Should().Be( details );
            auditLog.Status.Should().Be( auditLogStatus );
        }
      
    }
}

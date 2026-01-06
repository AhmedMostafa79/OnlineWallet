using Castle.Core.Logging;
using Castle.Core.Resource;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using OnlineWallet.API.Controllers;
using OnlineWallet.Application.DTOs;
using OnlineWallet.Application.Interfaces;
using OnlineWallet.Application.Models;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OnlineWallet.Tests.UnitTests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<ICustomerService> _mockCustomerService;
        private readonly Mock<IAccountService> _mockAccountService;
        private readonly Mock<ILogger<CustomerController>> _mockLogger;
        private readonly CustomerController _controller;
        private readonly ClaimsPrincipal _mockUser;
        public CustomerControllerTests()
        {
            _mockCustomerService = new Mock<ICustomerService>();
            _mockAccountService = new Mock<IAccountService >();
            _mockLogger = new Mock<ILogger<CustomerController>>();

            _mockUser = new ClaimsPrincipal(
                new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Email,"test@gmail.com")
                }, "TestAuthentication"));

            _controller =
                 new CustomerController(
                 _mockCustomerService.Object,
                 _mockLogger.Object
                 )
                 {
                     ControllerContext=new ControllerContext
                     {
                         HttpContext = new DefaultHttpContext { User = _mockUser }
                     }
                 };
        }

        [Fact]
        public async Task CreateAccountAsync_WithValidUser_ShouldReturnCreatedAccount()
        {
            // Arrange
            var customerId= Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
           
            var createdAccount = new GetAccountDto
            (
                accountNumber: Guid.NewGuid(),
                isActive:true,
                balance:0,
                ownerId:customerId,
                dateOpened:DateTime.UtcNow
            );
            _mockCustomerService
                .Setup(x => x.CreateAccountAsync(customerId))
                .ReturnsAsync(createdAccount);

            // Act
            var result = await _controller.CreateAccountAsync();

            // Assert
            var okResult = result.Result as OkObjectResult;// casts returned object into Ok status code

            okResult.Should().NotBeNull();
            okResult.Value.Should().BeEquivalentTo(createdAccount);

            //Verify
            _mockCustomerService.Verify(x => x.CreateAccountAsync(
                customerId), Times.Once);
        }
        [Fact]
        public async Task CreateAccountAsync_WhenServiceThrowsException_ShouldReturnCode500()
        {
            // Arrange 
            _mockCustomerService
                .Setup(x=>x.CreateAccountAsync(It.IsAny<Guid>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act 
            var result= await _controller.CreateAccountAsync();
            var statusCodeResult = result.Result as ObjectResult;
           
            // Assert
            statusCodeResult.Should().NotBeNull();
            statusCodeResult.StatusCode.Should().Be(500);
            statusCodeResult.Value.Should().Be("Failed to create account");

            // Verify
            _mockCustomerService.Verify(x =>
        x.CreateAccountAsync(
           It.IsAny<Guid>()),
        Times.Once);
        }
        [Fact]
        public async Task UpdateProfileAsync_WithValidRequest_ShouldUpdateProfile()
        {
            // Arrange
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var request = new UpdateUserDto
            {
                FirstName = "Updated",
                LastName = "Name"
            };

            // Act
            var result= await _controller.UpdateMyProfileAsync(request);

            // Assert
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            okResult.Value.Should().BeEquivalentTo(new { message = "Successfully updated profile" });

            _mockCustomerService.Verify(x =>
            x.UpdateCustomerAsync(customerId, request), Times.Once());
        }
        [Fact]
        public async Task UpdateMyProfileAsync_WithInvalidModel_ShouldThrowException()
        {
            // Arrange
            var request = new UpdateUserDto();
            _controller.ModelState.AddModelError("Amount", "The Amount field is required.");
           
            // Act
            var result = await _controller.UpdateMyProfileAsync(request);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Should().NotBeNull();
        }
        [Fact]
        public async Task GetCurrentAccountAsync_WithExistingAccount_ShouldReturnCurrentAccount()
        {
            // Arrange
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
           
            var currentAccount = new GetAccountDto
            (
                accountNumber: Guid.NewGuid(),
                isActive: true,
                balance: 500,
                ownerId: customerId,
                dateOpened: DateTime.UtcNow
            );

            _mockCustomerService.Setup(x => x.GetCurrentAccountAsync(customerId))
                .ReturnsAsync(currentAccount);

            // Act
            var result = await _controller.GetMyCurrentAccountAsync();
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Value.Should().NotBeNull();
            okResult.Value.Should().Be(currentAccount);
            okResult.StatusCode.Should().Be(200);
            // Verify
            _mockCustomerService.Verify(x => x.GetCurrentAccountAsync(customerId)
            , Times.Once);
        }

        [Fact]
        public async Task GetCurrentAccountAsync_WithNonExistentAccount_ShouldReturnNotFoundRequest()
        {
            // Arrange
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            _mockCustomerService.Setup(x => x.GetCurrentAccountAsync(customerId))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.GetMyCurrentAccountAsync();
            var requestResult = result.Result as ObjectResult;

            // Assert
            requestResult.Should().NotBeNull();
            requestResult.StatusCode.Should().Be(404);//not found

            // Verify
            _mockCustomerService.Verify(x => x.GetCurrentAccountAsync(customerId), Times.Once);
        }
        [Fact]
        public async Task GetCurrentAccountBalanceAsync_WithExistentAccount_ShouldReturnBalance()
        {
            // Arrange 
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var balance = 1500.99m;

            _mockCustomerService.Setup(x => x.GetCurrentAccountBalanceAsync(customerId))
                .ReturnsAsync(balance);

            // Act
            var result = await _controller.GetCurrentAccountBalanceAsync();
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.Value.Should().Be(balance);
            okResult.StatusCode.Should().Be(200);
            // Verify
            _mockCustomerService.Verify(x => x.GetCurrentAccountBalanceAsync(customerId), Times.Once);
        }

        [Fact]
        public async Task GetCurrentAccountBalanceAsync_WhenUnauthorized_ShouldReturnUnAuthorizedRequest()
        {
            // Arrange 
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            _mockCustomerService.Setup(x => x.GetCurrentAccountBalanceAsync(customerId))
                .ThrowsAsync(new UnauthorizedAccessException("Unauthorized user"));

            // Act
            var result = await _controller.GetCurrentAccountBalanceAsync();
            var okResult = result.Result as UnauthorizedObjectResult;

            // Assert
            okResult.Value.Should().NotBeNull();
            okResult.StatusCode.Should().Be(401);
            // Verify
            _mockCustomerService.Verify(x => x.GetCurrentAccountBalanceAsync(customerId), Times.Once);
        }

        [Fact]
        public async Task GetMyTransactionHistoryAsync_WhenAuthorized_ShouldReturnHistory()
        {
            // Arrange
            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier)?.Value);
           
            var transactionHistory=new List<AuditLog>
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

            _mockCustomerService.Setup(x => x.GetTransactionHistoryAsync(customerId))
                .ReturnsAsync(transactionHistory);


            // Act
            var result = await _controller.GetMyTransactionsHistoryAsync();
            var okResult = result.Result as OkObjectResult;

            // Assert
            okResult!.Value.Should().NotBeNull();
            okResult.Value.Should().BeEquivalentTo(transactionHistory);
            okResult.StatusCode.Should().Be(200);
            // Verify 
            _mockCustomerService.Verify(x => x.GetTransactionHistoryAsync(customerId),
                Times.Once);
        }

        [Fact]
        public async Task TransferFromMyAccountAsync_WithValidData_ShouldTransferAmount()
        {
            // Arrange
            var transferRequest = new TransferRequestModel
            {
                ToAccount = Guid.NewGuid(),
                Amount = 400.99m
            };

            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier).Value);

            _mockCustomerService.Setup(x => 
            x.TransferAsync(customerId, transferRequest))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.TransferFromMyAccountAsync(transferRequest);
            var okResult = result as OkObjectResult;

            // Assert
            okResult.Value.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);
        }
        [Fact]
        public async Task TransferFromMyAccountAsync_WithNonExistentDestination_ShouldReturnNotFoundRequest()
        {
            // Arrange
            var transferRequest = new TransferRequestModel
            {
                ToAccount = Guid.NewGuid(),
                Amount = 400.99m
            };

            var customerId = Guid.Parse(_mockUser.FindFirst(ClaimTypes.NameIdentifier).Value);

            _mockCustomerService.Setup(x =>
            x.TransferAsync(customerId, transferRequest))
                .ThrowsAsync(new KeyNotFoundException("Destination account not found"));

            // Act
            var result = await _controller.TransferFromMyAccountAsync(transferRequest);
            var requestResult = result as NotFoundObjectResult;

            // Assert
            requestResult.Value.Should().NotBeNull();
            requestResult.StatusCode.Should().Be(404);//not found

            _mockCustomerService.Verify(x =>
            x.TransferAsync(customerId, transferRequest),
            Times.Once);
                
        }
        [Fact]
        public async Task TransferFromMyAccountAsync_WithInvalidModelState_ShouldReturnBadRequest()
        {
            // Arrange
            var transferRequest = new TransferRequestModel
            {
                ToAccount = Guid.NewGuid()
                // Amount is missing
            };

            _controller.ModelState.AddModelError("Amount", "The Amount field is required.");

            // Act
            var result = await _controller.TransferFromMyAccountAsync(transferRequest);
            var badRequestResult = result as BadRequestObjectResult;

            // Assert
            badRequestResult.Should().NotBeNull();
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().NotBeNull();
        }
    }
}

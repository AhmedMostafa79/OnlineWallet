using OnlineWallet.Infrastructure.Interfaces;
using OnlineWallet.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Infrastructure.Repositories;
using OnlineWallet.Domain.Models;
using OnlineWallet.Domain.Enums;
using FluentAssertions;

namespace OnlineWallet.Tests.IntegrationTesting.Repositories
{
    public class UsersRepositoryTests : IDisposable
    {
        private readonly WalletDbContext _dbContext;
        private readonly IUsersRepository _repository;

        public UsersRepositoryTests()
        {

            //put configuration/ connection string here for simplicity
            var testId = Guid.NewGuid().ToString()[..8];
            var connectionString = $"Server=localhost;Database=Wallet_Test_{testId};Trusted_Connection=True;TrustServerCertificate=True;";
            
            var options = new DbContextOptionsBuilder<WalletDbContext>()
                .UseSqlServer(connectionString).Options;

            _dbContext = new WalletDbContext(options);
            _repository = new UsersRepository(_dbContext);
            InitializeDatabase();
        }
        public void InitializeDatabase()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
        }
        [Fact]
        public async Task AddAsync_ShouldAddUserToDatabase()
        {
            // Arrange
            var user = new User
                (
                id: Guid.NewGuid(),
                phoneNumber: "00123456789",
                email: "test@gmail.com",
                firstName: "Test",
                lastName: "User",
                passwordHash:"12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                );

            // Act
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var savedUser = await _repository.GetByIdAsync(user.Id);

            // Assert
            savedUser.Should().NotBeNull();
            savedUser.Should().Be(user);
        }
        [Fact]
        public async Task GetAllAsync_ShouldReturnAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User
                (
                id: Guid.NewGuid(),
                phoneNumber: "00123456789",
                email: "test@gmail.com",
                firstName: "Test",
                lastName: "User",
                passwordHash:"12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                ),
                new User
                (
                id: Guid.NewGuid(),
                phoneNumber: "01234567899",
                email: "test2@gmail.com",
                firstName: "ANY",
                lastName: "THING",
                passwordHash:"12354",
                role: UserRole.Admin,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
                )
            };

            // Act
            foreach (var user in users) {
                await _repository.AddAsync(user);
            }
            await _dbContext.SaveChangesAsync();


            var existingUsers= await _repository.GetAllAsync();

            // Assert
            existingUsers.Should().HaveCount(users.Count());
            existingUsers.Should().BeEquivalentTo(users);
        }
        [Fact]
        public async Task GetAllCustomers_ShouldReturnAllCustomers()
        {
            // Arrange
            var users = new List<User>
            {
                new User            //Customer
                (
                id: Guid.NewGuid(),
                phoneNumber: "00123456789",
                email: "customer@gmail.com",
                firstName: "Test",
                lastName: "Customer",
                passwordHash:"12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                ),
                new User            //Admin
                (
                id: Guid.NewGuid(),
                phoneNumber: "01234567899",
                email: "admin@gmail.com",
                firstName: "Test",
                lastName: "Admin",
                passwordHash:"12354",
                role: UserRole.Admin,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
                ),
                new User        //Customer
                (
                id: Guid.NewGuid(),
                phoneNumber: "01123456789",
                email: "customer2@gmail.com",
                firstName: "Test",
                lastName: "Customer2",
                passwordHash:"12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-20))
                )
            };

            // Act
            foreach(var user in users)
            {
                await _repository.AddAsync(user);
            }
            await _dbContext.SaveChangesAsync();

            var existingUsers = await _repository.GetAllCustomersAsync();

            // Assert
            existingUsers.Should().HaveCount(2);
            existingUsers.Where(u => u.Role == UserRole.Customer).Should().HaveCount(2);
        }
        [Fact]
        public async Task GetByIdAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var userId=Guid.NewGuid();
            var user = new User            //Customer
                (
                id:userId,
                phoneNumber: "00123456789",
                email: "test@gmail.com",
                firstName: "Test",
                lastName: "User",
                passwordHash: "12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                );
            
            // Act
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var existingUser = await _repository.GetByIdAsync(userId);

            // Assert
            existingUser.Should().NotBeNull(); ;
            existingUser.Should().Be(user);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userId=Guid.NewGuid();

            // Act
            var act=await _repository.GetByIdAsync(userId);

            // Assert
            act.Should().BeNull();
        }
        [Fact]
        public async Task FindByEmailAsync_WithExistingUser_ShouldReturnUser()
        {
            // Arrange
            var userEmail = "test@gmail.com";
            var user = new User            //Customer
                (
                id:Guid.NewGuid(),
                phoneNumber: "00123456789",
                email: userEmail,
                firstName: "Test",
                lastName: "User",
                passwordHash: "12354",
                role: UserRole.Customer,
                dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
                );

            // Act
            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            var existingUser = await _repository.FindByEmailAsync(userEmail);

            // Assert
            existingUser.Should().NotBeNull(); ;
            existingUser.Should().Be(user);
        }
        [Fact]
        public async Task FindByEmailAsync_WithNonExistentUser_ShouldReturnNull()
        {
            // Arrange
            var userEmail = "user@gmail.com";

            // Act
            var act = await _repository.FindByEmailAsync(userEmail);

            // Assert
            act.Should().BeNull();
        }
        [Fact]
        public async Task Update_WithExistingUser_ShouldUpdateUser()
        {
            // Arrange

            var user = new User            //Customer
             (
             id: Guid.NewGuid(),
             phoneNumber: "00123456789",
             email: "user@gmail.com",
             firstName: "Test",
             lastName: "User",
             passwordHash: "12354",
             role: UserRole.Customer,
             dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
             );

            await _repository.AddAsync(user);
            await _dbContext.SaveChangesAsync();


            // Act

            var userToUpdate = await _repository.GetByIdAsync(user.Id);
            userToUpdate.Should().NotBeNull();

            string firstNameBefore = userToUpdate.FirstName; // "Test"
            string lastNameBefore = userToUpdate.LastName;   // "User"

            userToUpdate.FirstName = "Ahmed";
            userToUpdate.LastName = "Mustafa";

            //_repository.Update(userToUpdate); --> //not necessary as EF Core tracks changes automatically
            await _dbContext.SaveChangesAsync();


            var userAfter = await _repository.GetByIdAsync(user.Id);

            // Assert
            userAfter.FirstName.Should().Be("Ahmed");
            userAfter.LastName.Should().Be("Mustafa");

            userAfter.FirstName.Should().NotBe("Test");
            userAfter.LastName.Should().NotBe("User");
        }
        [Fact]
        public async Task Delete_WithExistingUser_ShouldDeleteUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User            //Customer
             (
             id:userId,
             phoneNumber: "00123456789",
             email: "user@gmail.com",
             firstName: "Test",
             lastName: "User",
             passwordHash: "12354",
             role: UserRole.Customer,
             dateOfBirth: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
             );

            await _repository.AddAsync(user);
             await _dbContext.SaveChangesAsync();

            // Act

            _repository.Delete(user);
            await _dbContext.SaveChangesAsync();

            var existingUser = await _repository.GetByIdAsync(userId);
            // Assert
            existingUser.Should().BeNull();

        }
        public void Dispose()
        {
            try
            {
                _dbContext.Database.EnsureDeleted();
            }
            catch
            {

            }
            finally
            {
            _dbContext.Dispose();
            }
        }
    }
}

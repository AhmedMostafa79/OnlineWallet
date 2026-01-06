using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineWallet.Domain.Enums;
using OnlineWallet.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineWallet.Infrastructure
{
    public class DatabaseSeeder
    {
        private readonly WalletDbContext _context;
        private readonly PasswordHasher<object> _passHasher;
        public DatabaseSeeder(WalletDbContext context, PasswordHasher<object> passHasher)
        {
            _context = context;
            _passHasher = passHasher;
        }
        public async Task SeedManagerAsync()
        {
            var managerExists = await _context.Users.
                AnyAsync(u => u.Role == UserRole.Manager);

            if (managerExists)
                return;

            var managerId = Guid.NewGuid();
            var managerPassword = "Manager@279";
            var manager=new User
                (
                    id: managerId,
                    firstName: "Manager",
                    lastName: "User",
                    email: "manager@gmail.com",
                    phoneNumber: "01234567890",
                    passwordHash:_passHasher.HashPassword(null,managerPassword),
                    dateOfBirth: new DateOnly(1990,1,1),
                    role: UserRole.Manager
                );
            manager.CurrentAccountNumber = null;
            await _context.Users.AddAsync(manager);
            await _context.SaveChangesAsync();
        }
    }
}

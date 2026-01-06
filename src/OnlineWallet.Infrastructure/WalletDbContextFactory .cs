using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OnlineWallet.Infrastructure
{
    public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
    {
        public WalletDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WalletDbContext>();
            optionsBuilder.UseSqlServer("Server=.;Database=OnlineWalletDb;Trusted_Connection=True;TrustServerCertificate=True");

            return new WalletDbContext(optionsBuilder.Options);
        }
    }
}

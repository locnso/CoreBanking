using CoreBanking.Infrastructure.Entity;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.Infrastructure.Data;

public class CoreBankingDbContext : DbContext
{
    public CoreBankingDbContext(DbContextOptions<CoreBankingDbContext> option) : base(option)
    {

    }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
}

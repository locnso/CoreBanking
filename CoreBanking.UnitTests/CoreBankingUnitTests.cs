using CoreBanking.API.Apis;
using CoreBanking.API.Services;
using CoreBanking.Infrastructure.Data;
using CoreBanking.Infrastructure.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreBanking.UnitTests
{
    public class CoreBankingUnitTests
    {
        private SqliteConnection _sqliteConnection = default!;
        private DbContextOptions<CoreBankingDbContext> _dbContextOptions = default!;

        [Fact]
        public async Task Create_Customer_Test()
        {
            // Arrange
            _sqliteConnection = new SqliteConnection("data source=:memory:");
            _sqliteConnection.Open();
            _dbContextOptions = new DbContextOptionsBuilder<CoreBankingDbContext>()
                .UseSqlite(_sqliteConnection).Options;
            using var _dbContext = new CoreBankingDbContext(_dbContextOptions);
            _dbContext.Database.EnsureCreated();

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "locnso",
                Address = "nam dinh"
            };
            var services = new CoreBankingServices(_dbContext, NullLogger<CoreBankingServices>.Instance);

            // act
            var res = CoreBankingApi.CreateCustomer(services, customer);

            // Assert
            Assert.NotNull(res);

            var addedCustomer = await _dbContext.Customers.FindAsync(customer.Id);

            Assert.NotNull(addedCustomer);
            Assert.Equal(customer.Name, addedCustomer.Name);
            Assert.Equal(customer.Address, addedCustomer.Address);
            Assert.Equal(customer.Accounts.Count, addedCustomer.Accounts.Count);
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(10000)]
        [InlineData(500)]
        [InlineData(999999999.99)]
        public void Create_Customer_And_Deposit_Test(decimal deposit)
        {
            // Arrange
            _sqliteConnection = new SqliteConnection("data source=:memory:");
            _sqliteConnection.Open();
            _dbContextOptions = new DbContextOptionsBuilder<CoreBankingDbContext>()
                .UseSqlite(_sqliteConnection).Options;
            using var _dbContext = new CoreBankingDbContext(_dbContextOptions);
            _dbContext.Database.EnsureCreated();

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "locnso",
                Address = "nam dinh"
            };
            var services = new CoreBankingServices(_dbContext, NullLogger<CoreBankingServices>.Instance);

            // act
            var res = CoreBankingApi.CreateCustomer(services, customer);

            // Assert
            Assert.NotNull(res);

            var addedCustomer = _dbContext.Customers.Find(customer.Id);
            Assert.NotNull(addedCustomer);

            var account = new Account
            {
                Id = Guid.NewGuid(),
                CustomerId = addedCustomer.Id,
                Balance = 0
            };
            var addedAccount = CoreBankingApi.CreateAccount(services, account);

            Assert.NotNull(addedAccount);

            var depositRequest = new DepositionRequest
            {
                Amount = deposit
            };
            var result = CoreBankingApi.Deposit(services, account.Id, depositRequest);
            Assert.NotNull(result);

            var updatedAccount = _dbContext.Accounts.FirstOrDefault(a => a.Id == account.Id);
            Assert.NotNull(updatedAccount);
            Assert.Equal(depositRequest.Amount, updatedAccount.Balance);

            var transaction = _dbContext.Transactions.FirstOrDefault(ts => ts.AccountId == account.Id);
            Assert.NotNull(transaction);
            Assert.Equal(depositRequest.Amount, transaction.Amount);
        }
    }
}
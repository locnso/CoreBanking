using CoreBanking.API.Apis;
using CoreBanking.API.Services;
using CoreBanking.Infrastructure.Data;
using CoreBanking.Infrastructure.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CoreBanking.UnitTests
{
    public class CoreBankingUnitTests : IClassFixture<SqliteFixture>
    {
        private readonly SqliteFixture _fixture;

        public CoreBankingUnitTests(SqliteFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Create_Customer_Test()
        {
            // Arrange
            using var _dbContext = new CoreBankingDbContext(_fixture.contextOptions);

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
            using var _dbContext = new CoreBankingDbContext(_fixture.contextOptions);

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

        [Fact]
        public async Task Create_Customer_And_Transfer_Test()
        {
            // Arrange
            using var dbContext = new CoreBankingDbContext(_fixture.contextOptions);
            var services = new CoreBankingServices(dbContext, NullLogger<CoreBankingServices>.Instance);

            var accountSend = new Account
            {
                Id = Guid.NewGuid(),
                Balance = 2000
            };
            var accountReceive = new Account
            {
                Id = Guid.NewGuid(),
                Balance = 500
            };

            await Create_Customer_And_Transfer_Arrange(services, accountSend, accountReceive);

            var transferRequest = new TransferRequest
            {
                Amount = 1000,
                DestinationAccountNumber = accountReceive.Number
            };

            // act
            var res = CoreBankingApi.Transfer(services, accountSend.Id, transferRequest);

            // Assert
            Assert.NotNull(res);

            var updatedSendAccount = dbContext.Accounts.Find(accountSend.Id);
            Assert.NotNull(updatedSendAccount);
            Assert.Equal(accountSend.Balance - transferRequest.Amount, updatedSendAccount.Balance);

            var updatedReceiveAccount = dbContext.Accounts.Find(accountReceive.Id);
            Assert.NotNull(updatedReceiveAccount);
            Assert.Equal(accountReceive.Balance + transferRequest.Amount, updatedReceiveAccount.Balance);

            var transactionSend = dbContext.Transactions.FirstOrDefault(ts => ts.AccountId == accountSend.Id && ts.Type == TransactionTypes.Withdraw);
            Assert.NotNull(transactionSend);
            Assert.Equal(transferRequest.Amount, transactionSend.Amount);

            var transactionReceive= dbContext.Transactions.FirstOrDefault(ts => ts.AccountId == accountReceive.Id && ts.Type == TransactionTypes.Deposit);
            Assert.NotNull(transactionReceive);
            Assert.Equal(transferRequest.Amount, transactionReceive.Amount);
        }

        private static async Task Create_Customer_And_Transfer_Arrange(CoreBankingServices services, Account accountSend, Account accountReceive)
        {

            var customerSend = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "locnso",
                Address = "nam dinh"
            };
            var customerReceive = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "John Doe",
                Address = "123 Main St"
            };
            var addedCustomerSend = CoreBankingApi.CreateCustomer(services, customerSend);
            Assert.NotNull(addedCustomerSend);
            var addedCustomerReceive = CoreBankingApi.CreateCustomer(services, customerReceive);
            Assert.NotNull(addedCustomerReceive);

            accountSend.CustomerId = customerSend.Id;
            accountReceive.CustomerId = customerReceive.Id;

            var addedAccountSend = await CoreBankingApi.CreateAccount(services, accountSend);
            Assert.NotNull(addedAccountSend);

            var addedAccountReceive = await CoreBankingApi.CreateAccount(services, accountReceive);
            Assert.NotNull(addedAccountReceive);

            services.DbContext.ChangeTracker.Clear();
        }
    }
}
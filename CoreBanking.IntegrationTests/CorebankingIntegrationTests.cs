using CoreBanking.API.Apis;
using CoreBanking.Infrastructure.Entity;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace CoreBanking.IntegrationTests.Tests
{
    public class CorebankingIntegrationTests
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

        // Instructions:
        // 1. Add a project reference to the target AppHost project, e.g.:
        //
        //    <ItemGroup>
        //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
        //    </ItemGroup>
        //
        // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' to match your AppHost project:
        //
        [Fact]
        public async Task CoreBankingIntegrationTestFlow()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CoreBanking_AppHost>(cancellationToken);
            appHost.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                // Override the logging filters from the app's configuration
                logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
                // To output logs to the xUnit.net ITestOutputHelper, consider adding a package from https://www.nuget.org/packages?q=xunit+logging
            });
            appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
            {
                clientBuilder.AddStandardResilienceHandler();
            });

            await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
            await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            // Act
            using var httpClient = app.CreateHttpClient("corebanking-api");
            await app.ResourceNotifications.WaitForResourceHealthyAsync("corebanking-api", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

            // create customer
            var customer1 = new Customer()
            {
                Id = Guid.NewGuid(),
                Name = "locnso",
                Address = "Nam dinh"
            };
            var customer2 = new Customer()
            {
                Id = Guid.NewGuid(),
                Name = "minh anh",
                Address = "Ha Noi"
            };

            var result1 = await httpClient.PostAsJsonAsync("api/v1/corebanking/customers", customer1);
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            var result2 = await httpClient.PostAsJsonAsync("api/v1/corebanking/customers", customer2);
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

            //Get customer by id
            result1 = await httpClient.GetAsync($"api/v1/corebanking/customers/{customer1.Id}");
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            var customer1Response = await result1.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customer1Response);
            Assert.Equal(customer1.Id, customer1Response.Id);
            Assert.Equal(customer1.Name, customer1Response.Name);
            Assert.Equal(customer1.Address, customer1Response.Address);

            result2 = await httpClient.GetAsync($"api/v1/corebanking/customers/{customer2.Id}");
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

            var customer2Response = await result2.Content.ReadFromJsonAsync<Customer>();
            Assert.NotNull(customer2Response);
            Assert.Equal(customer2.Id, customer2Response.Id);
            Assert.Equal(customer2.Name, customer2Response.Name);
            Assert.Equal(customer2.Address, customer2Response.Address);

            //create account
            var account1 = new Account()
            {
                Id = Guid.NewGuid(),
                CustomerId = customer1.Id,
                Balance = 2000
            };
            var account2 = new Account()
            {
                Id = Guid.NewGuid(),
                CustomerId = customer2.Id,
                Balance = 500
            };

            result1 = await httpClient.PostAsJsonAsync("api/v1/corebanking/accounts", account1);
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            result2 = await httpClient.PostAsJsonAsync("api/v1/corebanking/accounts", account2);
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

            //Get account by accountNumber
            var account1Added = await result1.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account1Added);
            result1 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account1Added.Number}");
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            var account1Response = await result1.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account1Response);
            Assert.Equal(account1.Id, account1Response.Id);
            Assert.Equal(account1.CustomerId, account1Response.CustomerId);
            Assert.Equal(account1.Balance, account1Response.Balance);

            var account2Added = await result2.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account2Added);
            result2 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account2Added.Number}");
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);

            var account2Response = await result2.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account2Response);
            Assert.Equal(account2.Id, account2Response.Id);
            Assert.Equal(account2.CustomerId, account2Response.CustomerId);
            Assert.Equal(account2.Balance, account2Response.Balance);

            //withdraw
            var withdrawRequest = new WithdrawalRequest
            {
                Amount = 500
            };
            result1 = await httpClient.PutAsJsonAsync($"api/v1/corebanking/accounts/{account1Added.Id}/withdraw", withdrawRequest);
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            account1Added = await result1.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account1Added);
            Assert.Equal(account1.Balance - withdrawRequest.Amount, account1Added.Balance);

            //deposit
            var depositRequest = new DepositionRequest
            {
                Amount = 300
            };
            result1 = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account1Added.Id}/deposit", depositRequest);
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            var account1Updated = await result1.Content.ReadFromJsonAsync<Account>();
            Assert.NotNull(account1Updated);
            Assert.Equal(account1Added.Balance + depositRequest.Amount, account1Updated.Balance);

            //transfer
            result1 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account1Added.Number}");
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
            account1Added = await result1.Content.ReadFromJsonAsync<Account>();

            result2 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account2Added.Number}");
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);
            account2Added = await result2.Content.ReadFromJsonAsync<Account>();

            var transferRequest = new TransferRequest
            {
                DestinationAccountNumber = account2Added!.Number,
                Amount = 200
            };
            result1 = await httpClient.PutAsJsonAsync($"/api/v1/corebanking/accounts/{account1Added!.Id}/transfer", transferRequest);
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);

            result1 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account1Added.Number}");
            var account1Transfer = await result1.Content.ReadFromJsonAsync<Account>();

            result2 = await httpClient.GetAsync($"api/v1/corebanking/accounts/{account2Added.Number}");
            var account2Transfer = await result2.Content.ReadFromJsonAsync<Account>();

            Assert.Equal(account1Added.Balance - transferRequest.Amount, account1Transfer!.Balance);
            Assert.Equal(account2Added.Balance + transferRequest.Amount, account2Transfer!.Balance);
        }
    }
}

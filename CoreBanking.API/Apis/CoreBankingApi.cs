using CoreBanking.API.Models;
using CoreBanking.API.Services;
using CoreBanking.Infrastructure.Entity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CoreBanking.API.Apis;

public static class CoreBankingApi
{
    public static IEndpointRouteBuilder MapCoreBankingApi(this IEndpointRouteBuilder builder)
    {
        var vApi = builder.NewVersionedApi("corebanking");
        var v1 = vApi.MapGroup("api/v{version:apiVersion}/corebanking").HasApiVersion(1, 0);

        v1.MapGet("/customers", GetCustomers);
        v1.MapPost("/customers", CreateCustomer);

        v1.MapGet("/accounts", GetAccounts);
        v1.MapPost("/accounts", CreateAccount);
        v1.MapPut("/accounts/{id:guid}/deposit", Deposit);
        v1.MapPut("/accounts/{id:guid}/withdraw", Withdraw);
        v1.MapPut("/accounts/{id:guid}/transfer", Transfer);

        return builder;
    }

    private static async Task<Results<Ok, BadRequest>> Transfer(
        [AsParameters] CoreBankingServices services,
        Guid id, TransferRequest transfer)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogError("Account id can not be empty");
            return TypedResults.BadRequest();
        }

        if (transfer.Amount <= 0)
        {
            services.Logger.LogError("Transfer amount must be greater than zero");
            return TypedResults.BadRequest();
        }

        var account = await services.DbContext.Accounts.FindAsync(id);
        if (account == null)
        {
            services.Logger.LogError("Account not found");
            return TypedResults.BadRequest();
        }

        if (account.Balance < transfer.Amount)
        {
            services.Logger.LogError("Insufficient balance");
            return TypedResults.BadRequest();
        }

        var destinationAccount = await services.DbContext.Accounts
            .FirstOrDefaultAsync(a => a.Number == transfer.DestinationAccountNumber);
        if (destinationAccount == null)
        {
            services.Logger.LogError("Destination account not found");
            return TypedResults.BadRequest();
        }

        account.Balance -= transfer.Amount;
        destinationAccount.Balance += transfer.Amount;
        try
        {
            var now = DateTime.UtcNow;
            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Amount = transfer.Amount,
                Type = TransactionTypes.Withdraw,
                DateUtc = now
            });

            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = destinationAccount.Id,
                Amount = transfer.Amount,
                Type = TransactionTypes.Deposit,
                DateUtc = now
            });

            services.DbContext.Accounts.Update(account);
            services.DbContext.Accounts.Update(destinationAccount);

            await services.DbContext.SaveChangesAsync();
            services.Logger.LogInformation("Transfer transaction created successfully");

            return TypedResults.Ok();
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating transaction");
            return TypedResults.BadRequest();
        }
    }

    private static async Task<Results<Ok<Account>, BadRequest>> Withdraw(
        [AsParameters] CoreBankingServices services,
        Guid id, WithdrawalRequest withdrawal)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogError("Account id can not be empty");
            return TypedResults.BadRequest();
        }

        if (withdrawal.Amount <= 0)
        {
            services.Logger.LogError("Withdrawal amount must be greater than zero");
            return TypedResults.BadRequest();
        }

        var account = await services.DbContext.Accounts.FindAsync(id);
        if (account == null)
        {
            services.Logger.LogError("Account not found");
            return TypedResults.BadRequest();
        }

        account.Balance -= withdrawal.Amount;
        if (account.Balance < 0)
        {
            services.Logger.LogError("Insufficient balance");
            return TypedResults.BadRequest();
        }

        try
        {
            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Amount = withdrawal.Amount,
                Type = TransactionTypes.Withdraw,
                DateUtc = DateTime.UtcNow
            });

            services.DbContext.Accounts.Update(account);
            await services.DbContext.SaveChangesAsync();

            services.Logger.LogInformation("Withdrawal transaction created successfully");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating transaction");
            return TypedResults.BadRequest();
        }

        return TypedResults.Ok(account);
    }

    private static async Task<Results<Ok<Account>, BadRequest>> Deposit(
        [AsParameters] CoreBankingServices services,
        Guid id, DepositionRequest deposition)
    {
        if (id == Guid.Empty)
        {
            services.Logger.LogError("Account id can not be empty");
            return TypedResults.BadRequest();
        }

        if (deposition.Amount <= 0)
        {
            services.Logger.LogError("Deposit amount must be greater than zero");
            return TypedResults.BadRequest();
        }

        var account = await services.DbContext.Accounts.FindAsync(id);
        if (account == null)
        {
            services.Logger.LogError("Account not found");
            return TypedResults.BadRequest();
        }

        account.Balance += deposition.Amount;

        try
        {
            services.DbContext.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                Amount = deposition.Amount,
                Type = TransactionTypes.Deposit,
                DateUtc = DateTime.UtcNow
            });

            services.DbContext.Accounts.Update(account);
            await services.DbContext.SaveChangesAsync();

            services.Logger.LogInformation("Deposit transaction created successfully");
        }
        catch (Exception ex)
        {
            services.Logger.LogError(ex, "Error creating transaction");
            return TypedResults.BadRequest();
        }

        return TypedResults.Ok(account);
    }

    #region Account
    private static async Task<Results<Ok<Account>, BadRequest>> CreateAccount(
        [AsParameters] CoreBankingServices services,
        [FromBody] Account account)
    {
        if (account.CustomerId == Guid.Empty)
        {
            services.Logger.LogError("CustomerId can not be empty");
            return TypedResults.BadRequest();
        }

        account.Number = GenerateAccountNumber();
        account.Balance = 0;

        if (account.Id == Guid.Empty)
        {
            account.Id = Guid.NewGuid();
        }
        services.DbContext.Accounts.Add(account);
        await services.DbContext.SaveChangesAsync();

        services.Logger.LogInformation("Account created successfully");
        return TypedResults.Ok(account);
    }

    private static async Task<Ok<PaginationResponse<Account>>> GetAccounts(
        [AsParameters] CoreBankingServices services,
        [AsParameters] PaginationRequest pagination,
        Guid? customerId = null)
    {
        IQueryable<Account> accounts = services.DbContext.Accounts;
        if (customerId != null)
        {
            accounts = accounts.Where(a => a.CustomerId == customerId);
        }
        return TypedResults.Ok(new PaginationResponse<Account>(
            pagination.PageIndex,
            pagination.PageSize,
            await accounts.CountAsync(),
            await accounts
            .OrderBy(c => c.Number)
            .Skip(pagination.PageIndex * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync()
            ));
    }
    #endregion

    #region Customer
    private static async Task<Results<Ok<Customer>, BadRequest>> CreateCustomer(
        [AsParameters] CoreBankingServices services,
        [FromBody] Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.Name))
        {
            services.Logger.LogError("Customer name invalid");
            return TypedResults.BadRequest();
        }
        customer.Address ??= "";

        if (customer.Id == Guid.Empty)
        {
            customer.Id = Guid.NewGuid();
        }
        services.DbContext.Customers.Add(customer);
        await services.DbContext.SaveChangesAsync();

        services.Logger.LogInformation("Customer created successfully");
        return TypedResults.Ok(customer);
    }

    private static async Task<Ok<PaginationResponse<Customer>>> GetCustomers(
        [AsParameters] CoreBankingServices services,
        [AsParameters] PaginationRequest pagination)
    {
        return TypedResults.Ok(new PaginationResponse<Customer>(
            pagination.PageIndex,
            pagination.PageSize,
            await services.DbContext.Customers.CountAsync(),
            await services.DbContext.Customers
            .OrderBy(c => c.Name)
            .Skip(pagination.PageIndex * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync()
            ));
    }
    #endregion

    private static string GenerateAccountNumber()
    {
        return DateTime.UtcNow.Ticks.ToString();
    }
}

class DepositionRequest
{
    public decimal Amount { get; set; }
}

class WithdrawalRequest
{
    public decimal Amount { get; set; }
}

class TransferRequest
{
    public decimal Amount { get; set; }
    public string DestinationAccountNumber { get; set; } = default!;
}

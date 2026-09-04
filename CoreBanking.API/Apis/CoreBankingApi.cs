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

    private static async Task Transfer(Guid id)
    {
        throw new NotImplementedException();
    }

    private static async Task Withdraw(Guid id)
    {
        throw new NotImplementedException();
    }

    private static async Task Deposit(Guid id)
    {
        throw new NotImplementedException();
    }

    private static async Task CreateAccount(HttpContext context)
    {
        throw new NotImplementedException();
    }

    private static async Task GetAccounts([AsParameters] PaginationRequest pagination)
    {
        throw new NotImplementedException();
    }

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
}

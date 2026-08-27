using CoreBanking.API.Apis;
using CoreBanking.API.Bootstraping;

namespace CoreBanking.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddApplicationServices();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        app.UseHttpsRedirection();
        app.MapCoreBankingApi();

        app.Run();
    }
}

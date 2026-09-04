using CoreBanking.Infrastructure.Data;

namespace CoreBanking.API.Services;

public class CoreBankingServices(CoreBankingDbContext dbContext, ILogger<CoreBankingServices> logger)
{
    public CoreBankingDbContext DbContext { get; set; } = dbContext;
    public ILogger<CoreBankingServices> Logger { get; set; } = logger;
}

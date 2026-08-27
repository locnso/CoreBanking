namespace CoreBanking.API.Services
{
    public class CoreBankingServices(ILogger<CoreBankingServices> logger)
    {
        public ILogger<CoreBankingServices> Logger { get; set; } = logger;
    }
}

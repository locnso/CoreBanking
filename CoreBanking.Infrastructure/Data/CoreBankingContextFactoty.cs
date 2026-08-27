using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreBanking.Infrastructure.Data;

public class CoreBankingContextFactoty : IDesignTimeDbContextFactory<CoreBankingDbContext>
{
    public CoreBankingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoreBankingDbContext>();
        optionsBuilder.UseNpgsql("host=localhost; database=corebanking; username=postgres; password =123");
        return new CoreBankingDbContext(optionsBuilder.Options);
    }
}

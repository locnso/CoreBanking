using CoreBanking.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreBanking.UnitTests
{
    public class SqliteFixture : IDisposable
    {
        public SqliteConnection Connection { get; set; } = default!;
        public DbContextOptions<CoreBankingDbContext> contextOptions { get; set; } = default!;

        public SqliteFixture()
        {
            Connection = new SqliteConnection("data source=:memory:");
            Connection.Open();
            contextOptions = new DbContextOptionsBuilder<CoreBankingDbContext>()
                .UseSqlite(Connection).Options;

            using var context = new CoreBankingDbContext(contextOptions);
            context.Database.EnsureCreated();
        }
        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}

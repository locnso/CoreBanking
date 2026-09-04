var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
                      .WithImageTag("17")
                      .WithVolume("corebanking-db", "/var/lib/postgresql/data")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgWeb();

var coreBankingDb = postgres.AddDatabase("corebanking-db", "corebanking");

var migrationService = builder.AddProject<Projects.CoreBanking_MigrationService>("corebanking-migrationservice")
    .WithReference(coreBankingDb)
    .WaitFor(coreBankingDb);

builder.AddProject<Projects.CoreBanking_API>("corebanking-api")
       .WithReference(coreBankingDb)
       .WaitFor(postgres)
       .WaitForCompletion(migrationService);

builder.Build().Run();

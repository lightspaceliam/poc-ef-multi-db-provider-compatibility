using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace POC.Entities.DbContexts;

/// <summary>
/// Used for generating database schema migrations only.
/// </summary>
public class OptimiserPgDbContextFactory : IDesignTimeDbContextFactory<OptimiserPgSqlDbContext>
{
    public OptimiserPgSqlDbContext CreateDbContext(string[] args)
    {
        /*
         * Local connection sting only. Demo purposes, never store database connection strings in code.
         * .NET provides many secure alternatives: UserSecrets, Environment Variables and more.
         */
        const string connectionString = "Host=localhost;Port=5432;Database=optimiser_pg_db_context;Username=postgres;Password=YourSecurePassword123;";

        var optionsBuilder = new DbContextOptionsBuilder<OptimiserPgSqlDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new OptimiserPgSqlDbContext(optionsBuilder.Options);
    }
}
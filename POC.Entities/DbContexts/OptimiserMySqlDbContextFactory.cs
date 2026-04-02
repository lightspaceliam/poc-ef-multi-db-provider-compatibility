using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace POC.Entities.DbContexts;

/// <summary>
/// Used for generating database schema migrations only.
/// </summary>
public class OptimiserMySqlDbContextFactory : IDesignTimeDbContextFactory<OptimiserMySqlDbContext>
{
    public OptimiserMySqlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OptimiserMySqlDbContext>();
        
        /*
         * Local connection sting only. Demo purposes, never store database connection strings in code.
         * .NET provides many secure alternatives: UserSecrets, Environment Variables and more. 
         */
        const string connectionString = "Server=localhost;Port=3306;Database=OptimiserMySqlDbContext;Uid=root;Pwd=YourSecurePassword123;";
        
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mysqlOptions => mysqlOptions.EnableRetryOnFailure());
        return new OptimiserMySqlDbContext(optionsBuilder.Options);
    }
}
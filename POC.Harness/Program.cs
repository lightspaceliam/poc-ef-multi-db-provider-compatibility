// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POC.Entities.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using POC.Entities;
using POC.Harness.Data;
using POC.Harness.Services;

Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT",     "Development");

var host = Host.CreateDefaultBuilder(args)
    // Setting DOTNET_ENVIRONMENT=Development (above) causes Host.CreateDefaultBuilder
    // to enable ValidateOnBuild=true. That validation instantiates TrialPgDbContext
    // to verify its options, which triggers the GlobalTypeMapper + HasPostgresEnum
    // conflict and throws. Disable it — this is a utility, not a production service.
    // .UseDefaultServiceProvider(options =>
    // {
    //     options.ValidateOnBuild = false;
    //     options.ValidateScopes  = false;
    // })
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddUserSecrets<Program>();
        config.AddEnvironmentVariables();
        var environment = context.HostingEnvironment;

        var basePath = environment.IsDevelopment()
            ? Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName).FullName
            : Directory.GetCurrentDirectory();
        config.SetBasePath(basePath);
    })
    .ConfigureLogging(logging =>
    {

    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddLogging();

        /*
         * Local connection sting only. Demo purposes, never store database connection strings in code.
         * .NET provides many secure alternatives: UserSecrets, Environment Variables and more.
         */
        const string pgSqlconnectionString =
            "Host=localhost;Port=5432;Database=optimiser_pg_db_context;Username=postgres;Password=YourSecurePassword123;";
        services.AddDbContext<OptimiserPgSqlDbContext>(options =>
        {
            options.UseNpgsql(
                pgSqlconnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
        });
        
        const string mySqlconnectionString =
            "Server=localhost;Port=3306;Database=OptimiserMySqlDbContext;Uid=root;Pwd=YourSecurePassword123;";
        services.AddDbContext<OptimiserMySqlDbContext>(options =>
        {
            options.UseMySql(
                mySqlconnectionString,
                ServerVersion.AutoDetect(mySqlconnectionString),
                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
        });
        
        services.AddScoped<PgSqlTrialEntityService>();
        services.AddScoped<MySqlTrialEntityService>();
        
    })
    .Build();

using var scope = host.Services.CreateScope();
var pgSqlTrialEntityService = scope.ServiceProvider.GetRequiredService<PgSqlTrialEntityService>();
var mySqlTrialEntityService = scope.ServiceProvider.GetRequiredService<MySqlTrialEntityService>();

const bool pgSqlRunCreate = false;
const bool pgSqlRunReadAllTrials = false;
const bool pgSqlRunReadAllWithCriterion = true;
const bool pgSqlRunAttemptInsertWithDuplicate = false;

const bool mySqlRunCreate = false;
const bool mySqlRunReadAllTrials = false;
const bool mySqlRunReadAllWithCriterion = true;
const bool mySqlRunAttemptInsertWithDuplicate = false;

#region PostgreSql Create

if (pgSqlRunCreate)
{
    foreach (var trial in nameof(OptimiserPgSqlDbContext).TrialsData())
    {
        await pgSqlTrialEntityService.CreateAsync(trial);
    }
}

#endregion

#region MySql Create

if (mySqlRunCreate)
{
    foreach (var trial in nameof(OptimiserMySqlDbContext).TrialsData())
    {
        await mySqlTrialEntityService.CreateAsync(trial);
    }
}

#endregion

#region PostgreSql Read All Trials

if (pgSqlRunReadAllTrials)
{
    var pgSqlTrials = await pgSqlTrialEntityService.ReadAllAsync();

    pgSqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"PgSql - Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion

#region MySql Read All Trials

if (mySqlRunReadAllTrials)
{
    var mySqlTrials = await mySqlTrialEntityService.ReadAllAsync();

    mySqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"MySql - Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion

#region PostgreSql Read All Trials With Criterion

if (pgSqlRunReadAllWithCriterion)
{
    var pgSqlTrials = await pgSqlTrialEntityService.ReadAllWithCriterionAsync();

    pgSqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"PgSql - Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion

#region MySql Read All Trials With Criterion

if (mySqlRunReadAllWithCriterion)
{
    var mySqlTrials = await mySqlTrialEntityService.ReadAllWithCriterionAsync();

    mySqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"MySql - Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion

#region PostgreSql Attempt Insert With Duplicate Type

//  Expecting:  23505: duplicate key value violates unique constraint "unique_criteria_trial_id_criteria_type"

if (pgSqlRunAttemptInsertWithDuplicate)
{
    var pgSqlTrial = await pgSqlTrialEntityService.FindTrialByNamAsync($"Trial One - Db: {nameof(OptimiserPgSqlDbContext)}");

    if (pgSqlTrial == null)
    {
        Console.WriteLine("PgSql - Trial One not found");
        return;
    }
    var trialId = pgSqlTrial.Id;
    var pgSqlCriteria = new Criteria
    {
        Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.MainEvent)}",
        Type = CriteriaTypes.MainEvent,
        TrialId = trialId
    };
    await pgSqlTrialEntityService.CreateCriteriaAsync(pgSqlCriteria);
}
#endregion Attempt Insert With Duplicate

#region MySql Attempt Insert With Duplicate Type

//  Expecting:  23505: duplicate key value violates unique constraint "unique_criteria_trial_id_criteria_type"

if (mySqlRunAttemptInsertWithDuplicate)
{
    var mySqlTrial = await mySqlTrialEntityService.FindTrialByNamAsync($"Trial One - Db: {nameof(OptimiserMySqlDbContext)}");

    if (mySqlTrial == null)
    {
        Console.WriteLine("MySql Trial One not found");
        return;
    }
    var trialId = mySqlTrial.Id;
    var mySqlCriteria = new Criteria
    {
        Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.MainEvent)}",
        Type = CriteriaTypes.MainEvent,
        TrialId = trialId
    };
    await mySqlTrialEntityService.CreateCriteriaAsync(mySqlCriteria);
}
#endregion Attempt Insert With Duplicate

Console.WriteLine("Hello, World!");
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
            "Host=localhost;Port=5432;Database=pg_db_context;Username=postgres;Password=YourSecurePassword123;";
        services.AddDbContext<PgSqlDbContext>(options =>
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
            "Server=localhost;Port=3306;Database=MySqlDbContext;Uid=root;Pwd=YourSecurePassword123;";
        services.AddDbContext<MySqlDbContext>(options =>
        {
            options.UseMySql(
                mySqlconnectionString,
                ServerVersion.AutoDetect(mySqlconnectionString),
                mysqlOptions => mysqlOptions.EnableRetryOnFailure());
        });
        
        services.AddScoped<PgSqlPatientEntityService>();
        services.AddScoped<MySqlPatientEntityService>();
        
    })
    .Build();

using var scope = host.Services.CreateScope();
var pgSqlTrialEntityService = scope.ServiceProvider.GetRequiredService<PgSqlPatientEntityService>();
var mySqlTrialEntityService = scope.ServiceProvider.GetRequiredService<MySqlPatientEntityService>();

const bool pgSqlRunCreate = false;
const bool pgSqlRunReadAllPatients = false;
const bool pgSqlRunReadAllWithIdentifiers = false;
const bool pgSqlRunAttemptInsertWithDuplicate = false;

const bool mySqlRunCreate = false;
const bool mySqlRunReadAllPatients = false;
const bool mySqlRunReadAllWithIdentifiers = false;
const bool mySqlRunAttemptInsertWithDuplicate = true;

#region PostgreSql Create

if (pgSqlRunCreate)
{
    foreach (var trial in nameof(PgSqlDbContext).TrialsData())
    {
        await pgSqlTrialEntityService.CreateAsync(trial);
    }
}

#endregion

#region MySql Create

if (mySqlRunCreate)
{
    foreach (var trial in nameof(MySqlDbContext).TrialsData())
    {
        await mySqlTrialEntityService.CreateAsync(trial);
    }
}

#endregion

#region PostgreSql Read All Trials

if (pgSqlRunReadAllPatients)
{
    var pgSqlTrials = await pgSqlTrialEntityService.ReadAllAsync();

    pgSqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"PgSql - Trial name: {trial.Name} Criterion: {trial.Identifiers.Count}");
    });
}
#endregion

#region MySql Read All Trials

if (mySqlRunReadAllPatients)
{
    var mySqlTrials = await mySqlTrialEntityService.ReadAllAsync();

    mySqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"MySql - Trial name: {trial.Name} Criterion: {trial.Identifiers.Count}");
    });
}
#endregion

#region PostgreSql Read All Trials With Criterion

if (pgSqlRunReadAllWithIdentifiers)
{
    var pgSqlTrials = await pgSqlTrialEntityService.ReadAllWithIdentifiersAsync();

    pgSqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"PgSql - Trial name: {trial.Name} Criterion: {trial.Identifiers.Count}");
    });
}
#endregion

#region MySql Read All Trials With Criterion

if (mySqlRunReadAllWithIdentifiers)
{
    var mySqlTrials = await mySqlTrialEntityService.ReadAllWithIdentifiersAsync();

    mySqlTrials.ForEach(trial =>
    {
        Console.WriteLine($"MySql - Trial name: {trial.Name} Criterion: {trial.Identifiers.Count}");
    });
}
#endregion

#region PostgreSql Attempt Insert With Duplicate Type

//  Expecting:  23505: duplicate key value violates unique constraint "unique_criteria_trial_id_criteria_type"

if (pgSqlRunAttemptInsertWithDuplicate)
{
    var pgSqlPatient = await pgSqlTrialEntityService.FindPatientByNamAsync($"Luke Skywalker - Db: {nameof(PgSqlDbContext)}");

    if (pgSqlPatient == null)
    {
        Console.WriteLine("PgSql Patient Luke Skywalker not found");
        return;
    }
    var patientId = pgSqlPatient.Id;
    var pgSqlCriteria = new Identifier
    {
        Description = $"{nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}",
        Code = "1",
        Use = Use.Official,
        PatientId = patientId
    };
    await pgSqlTrialEntityService.CreateIdentifierAsync(pgSqlCriteria);
}
#endregion Attempt Insert With Duplicate

#region MySql Attempt Insert With Duplicate Type

//  Expecting:  23505: duplicate key value violates unique constraint "unique_criteria_trial_id_criteria_type"

if (mySqlRunAttemptInsertWithDuplicate)
{
    var mySqlPatient = await mySqlTrialEntityService.FindPatientByNamAsync($"Luke Skywalker - Db: {nameof(MySqlDbContext)}");

    if (mySqlPatient == null)
    {
        Console.WriteLine("MySql Patient Luke Skywalker not found");
        return;
    }
    var patientId = mySqlPatient.Id;
    var mySqlCriteria = new Identifier
    {
        Description = $"{nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}",
        Code = "1",
        Use = Use.Official,
        PatientId = patientId
    };
    await mySqlTrialEntityService.CreateIdentifierAsync(mySqlCriteria);
}
#endregion Attempt Insert With Duplicate

Console.WriteLine("Hello, World!");
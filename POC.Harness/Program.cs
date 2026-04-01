// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using POC.Entities.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using POC.Entities;
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
        const string connectionString =
            "Host=localhost;Port=5432;Database=optimiser_pg_db_context;Username=postgres;Password=YourPassword123;";
        services.AddDbContext<OptimiserPgDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null);
                });
        });

        // No UseNpgsql here — OnConfiguring in TrialPgDbContext owns the connection string.
        // It resolves it via _tenantService.tenant.TrialDbConnectionString, which
        
        services.AddScoped<TrialEntityService>();
        
    })
    .Build();

using var scope = host.Services.CreateScope();
var trialEntityService = scope.ServiceProvider.GetRequiredService<TrialEntityService>();
const bool runCreate = false;
const bool runReadAllTrials = false;
const bool runReadAllWithCriterion = false;
const bool runAttemptInsertWithDuplicate = true;

#region Create

if (runCreate)
{
    var trials = new List<Trial>
    {
        new Trial
        {
            Name = "Trial One",
            StartDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 09:00:00"), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 10:00:00"), DateTimeKind.Utc),
            Criterion = new List<Criteria>
            {
                new Criteria{ Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Mainevent)}", Type = CriteriaTypes.Mainevent },
                new Criteria{ Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Inclusion)}", Type = CriteriaTypes.Inclusion },
                new Criteria{ Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Exclusion)}", Type = CriteriaTypes.Exclusion },
            }
        },
        new Trial
        {
            Name = "Trial Two",
            StartDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 11:00:00"), DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-02 11:30:00"), DateTimeKind.Utc),
            Criterion = new List<Criteria>
            {
                new Criteria{ Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Mainevent)}", Type = CriteriaTypes.Mainevent },
                new Criteria{ Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Inclusion)}", Type = CriteriaTypes.Inclusion }
            }
        }
    };

    foreach (var trial in trials)
    {
        await trialEntityService.CreateAsync(trial);
    }
}

#endregion Create

#region Read All Trials

if (runReadAllTrials)
{
    var trials = await trialEntityService.ReadAllAsync();

    trials.ForEach(trial =>
    {
        Console.WriteLine($"Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion All Trials

#region Read All Trials With Criterion

if (runReadAllWithCriterion)
{
    var trials = await trialEntityService.ReadAllWithCriterionAsync();

    trials.ForEach(trial =>
    {
        Console.WriteLine($"Trial name: {trial.Name} Criterion: {trial.Criterion.Count}");
    });
}
#endregion All Trials With Criterion

#region Attempt Insert With Duplicate

if (runAttemptInsertWithDuplicate)
{
    var trial = await trialEntityService.FindTrialByNamAsync("Trial One");

    if (trial == null)
    {
        Console.WriteLine("Trial One not found");
        return;
    }
    var trialId = trial.Id;
    var criteria = new Criteria
    {
        Description = $"{nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Mainevent)}",
        Type = CriteriaTypes.Mainevent
    };
    await trialEntityService.CreateCriteriaAsync(criteria);
}
#endregion Attempt Insert With Duplicate

Console.WriteLine("Hello, World!");
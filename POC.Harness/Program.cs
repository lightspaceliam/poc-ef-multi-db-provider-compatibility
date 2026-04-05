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
var pgSqlPatientEntityService = scope.ServiceProvider.GetRequiredService<PgSqlPatientEntityService>();
var mySqlPatientEntityService = scope.ServiceProvider.GetRequiredService<MySqlPatientEntityService>();

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
    foreach (var patient in nameof(PgSqlDbContext).PatientsData())
    {
        await pgSqlPatientEntityService.CreateAsync(patient);
    }
}

#endregion

#region MySql Create

if (mySqlRunCreate)
{
    foreach (var patient in nameof(MySqlDbContext).PatientsData())
    {
        await mySqlPatientEntityService.CreateAsync(patient);
    }
}

#endregion

#region PostgreSql Read All Patients

if (pgSqlRunReadAllPatients)
{
    var pgSqlPatients = await pgSqlPatientEntityService.ReadAllAsync();

    pgSqlPatients.ForEach(patient =>
    {
        Console.WriteLine($"PgSql - Patient name: {patient.Name} Identifiers: {patient.Identifiers.Count}");
    });
}
#endregion

#region MySql Read All Patients

if (mySqlRunReadAllPatients)
{
    var mySqlPatients = await mySqlPatientEntityService.ReadAllAsync();

    mySqlPatients.ForEach(patient =>
    {
        Console.WriteLine($"MySql - Patient name: {patient.Name} Identifier: {patient.Identifiers.Count}");
    });
}
#endregion

#region PostgreSql Read All Patients With Identifiers

if (pgSqlRunReadAllWithIdentifiers)
{
    var pgSqlPatients = await pgSqlPatientEntityService.ReadAllWithIdentifiersAsync();

    pgSqlPatients.ForEach(patient =>
    {
        Console.WriteLine($"PgSql - Patient name: {patient.Name} Identifiers: {patient.Identifiers.Count}");
    });
}
#endregion

#region MySql Read All Patients With Identifiers

if (mySqlRunReadAllWithIdentifiers)
{
    var mySqlPatients = await mySqlPatientEntityService.ReadAllWithIdentifiersAsync();

    mySqlPatients.ForEach(patient =>
    {
        Console.WriteLine($"MySql - Patient name: {patient.Name} Identifiers: {patient.Identifiers.Count}");
    });
}
#endregion

#region PostgreSql Attempt Insert With Duplicate Type

//  Expecting to throw unique constraint exception "unique_pgsql_identifier_code_use_patient_id"

if (pgSqlRunAttemptInsertWithDuplicate)
{
    var pgSqlPatient = await pgSqlPatientEntityService.FindPatientByNamAsync($"Luke Skywalker - Db: {nameof(PgSqlDbContext)}");

    if (pgSqlPatient == null)
    {
        Console.WriteLine("PgSql Patient Luke Skywalker not found");
        return;
    }
    var patientId = pgSqlPatient.Id;
    var pgSqlIdentifier = new Identifier
    {
        Description = $"{nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}",
        Code = "1",
        Use = Use.Official,
        PatientId = patientId
    };
    await pgSqlPatientEntityService.CreateIdentifierAsync(pgSqlIdentifier);
}
#endregion Attempt Insert With Duplicate

#region MySql Attempt Insert With Duplicate Type

//  Expecting to throw unique constraint exception "unique_mysql_identifier_code_use_patient_id"

if (mySqlRunAttemptInsertWithDuplicate)
{
    var mySqlPatient = await mySqlPatientEntityService.FindPatientByNamAsync($"Luke Skywalker - Db: {nameof(MySqlDbContext)}");

    if (mySqlPatient == null)
    {
        Console.WriteLine("MySql Patient Luke Skywalker not found");
        return;
    }
    var patientId = mySqlPatient.Id;
    var mySqlIdentifier = new Identifier
    {
        Description = $"{nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}",
        Code = "1",
        Use = Use.Official,
        PatientId = patientId
    };
    await mySqlPatientEntityService.CreateIdentifierAsync(mySqlIdentifier);
}
#endregion Attempt Insert With Duplicate

Console.WriteLine("Hello, World!");
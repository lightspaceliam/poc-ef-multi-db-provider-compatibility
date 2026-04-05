# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### EF Core CLI — must be version 9.0.11
Pomelo.EntityFrameworkCore.MySql 9.0.0 only supports up to .NET 9, so the EF CLI must stay at 9.0.11.

```bash
# Install (or downgrade from a newer version)
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 9.0.11
```

### Migrations — run from POC.Entities/
```bash
cd POC.Entities

dotnet ef migrations add Init --context PgSqlDbContext --output-dir Migrations/PgSql
dotnet ef migrations add Init --context MySqlDbContext --output-dir Migrations/MySql

dotnet ef migrations remove --context PgSqlDbContext
dotnet ef migrations remove --context MySqlDbContext
```

### Apply migrations / create databases
```bash
dotnet ef database update --context PgSqlDbContext
dotnet ef database update --context MySqlDbContext

dotnet ef database drop --context PgSqlDbContext
dotnet ef database drop --context MySqlDbContext
```

### Run the harness
```bash
cd POC.Harness
dotnet run
```

### Docker — start database servers
```bash
# PostgreSQL
docker run --name postgres-dev-sever \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourSecurePassword123 \
  -p 5432:5432 \
  -d postgres:latest

# MySQL
docker run --name mysql-dev-server \
  -e MYSQL_ROOT_PASSWORD=YourSecurePassword123 \
  -p 3306:3306 \
  -d mysql:latest
```

## Architecture

Two projects:
- **POC.Entities** — class library. Contains entities, enums, both DbContexts, design-time factories, and migrations.
- **POC.Harness** — console app. Bootstraps DI, registers both DbContexts, and runs CRUD operations against both databases.

### The multi-provider pattern

Entities (`Patient`, `Identifier`, `Use` enum) carry no database opinions. All provider-specific configuration lives in `OnModelCreating` of each DbContext:

| | `PgSqlDbContext` | `MySqlDbContext` |
|---|---|---|
| Table/column naming | lowercase (`patients`, `birth_date`) | PascalCase defaults |
| Enum storage | `varchar(50)` + `CHECK` constraint | Native MySQL `ENUM(...)` |
| Both use | `HasConversion<string>()` | `HasConversion<string>()` |

Each context also has a design-time factory (`PgDbContextFactory`, `MySqlDbContextFactory`) used exclusively by the EF CLI for migrations. Connection strings appear in three places: both factories and `Program.cs`.

### Feature flags in Program.cs

Operations in `Program.cs` are controlled by `const bool` flags at the top of the execution block. Toggle these to test different scenarios without commenting out code:

```csharp
const bool pgSqlRunCreate = false;
const bool pgSqlRunReadAllPatients = false;
// ...
const bool mySqlRunAttemptInsertWithDuplicate = true;
```

## Key Conventions

**PostgreSQL naming** — everything must be lowercase. `OnModelCreating` in `PgSqlDbContext` maps every table and column to lowercase explicitly. The EF-generated SQL uses double-quotes for any mixed-case identifier, so staying lowercase avoids quoting issues.

**Enum storage** — never store C# enum ordinals (integers). Always use `HasConversion<string>()` so the string member name is stored. PostgreSQL enforces valid values via a `CHECK` constraint; MySQL enforces them via a native `ENUM` column type.

**DateTime** — Npgsql 9 maps `DateTime` to `timestamp with time zone` and requires `DateTimeKind.Utc`. Always construct dates explicitly:
```csharp
new DateTime(1972, 1, 2, 0, 0, 0, DateTimeKind.Utc)
```
`DateTime.Parse(...)` without a kind produces `Unspecified`, which Npgsql 9 rejects.

**global.json** pins the SDK to `9.0.11` with `rollForward: latestMinor`. If the EF CLI version or provider versions appear incompatible, check this file first.

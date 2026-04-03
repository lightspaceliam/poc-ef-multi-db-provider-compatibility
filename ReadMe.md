# POC Database Entity Framework

## Tech Stack

- Entity Framework 9.0.11
- PostgreSql 9.0.11
- MySql latest 9.0.4
- .NET 9.0

**Compatibility Constraints:**

- Pomelo.EntityFrameworkCore.MySql 9.0.0 works with is .NET 8 LTS or 9.
- Npgsql.EntityFrameworkCore.PostgreSQL is compatible with .NET 10

Based on the above, I'm opting for .NET 9.

## Conventions

I've **ALWAYS** used MS SQL Server so I've listed some nuances I've come across when working with PostgreSql and MySql.

### PostgreSql

> Case Sensitivity 

New for me: everything in lowercase unless you want to wrap double quotes around everything.

```sql
-- mixed case
SELECT  *
FROM    "MyTableName"

--  lowercase
SELECT 	id
		, description
FROM    mytable;
```

> String Qualifier - "Double-Quotes"

```sql
SELECT 	id
		  , "description"
FROM    mytable;
```

### MySql

> Case Sensitivity

You can use whatever case you want.

```sql
SELECT 	Id 
		, Description
FROM 	MyTable
```

> String Qualifier - `Backtick`


```sql
SELECT 	Id 
		, `Description`
FROM 	MyTable
```

## Getting Started

This solution requires access to two database server types:

- PostgreSql
- MySql

I'm using Docker to host both servers. Connection strings are in the codebase in plain text, local connections only.

Please ensure both servers are available and work with the specified connection strings or update to what works for you.

Connection strings are stored:

- POC.Harness\Program.cs
- POC.Entities\DbContexts\OptimiserMySqlDbContextFactory.cs
- POC.Entities\DbContexts\OptimiserPgDbContextFactory.cs

You will need .NET 9 and EF CLI 9.0.11 and run EF CLI commands:

In this case I already had 10 installed and I needed to downgrade.

```bash
# If you are using a newer version Also, check if there is a global.json in the solution root if encountering any issues
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 9.0.11


# install if not already installed 
dotnet tool install --global dotnet-ef --version 9.0.11


# Create the database and schema
dotnet ef database update --context OptimiserPgSqlDbContext
dotnet ef database update --context OptimiserMySqlDbContext
```

Highly recommend you install https://dbeaver.io/ to read both database schemas without having to swap IDE's.

## Entity Framework CLI Commands

Generally there is only a single Fluent API in a project however, this composition was chosen to differentiate between MySql to PostgreSql.

In Terminal, PowerShell cd into: {your-directory}/poc-database-entity-framework/POC.Entities/

```bash
# Add a migration, select context and output directory
dotnet ef migrations add Init --context OptimiserPgSqlDbContext --output-dir Migrations/PgSql

# Remove a migration
dotnet ef migrations remove --context OptimiserPgSqlDbContext


dotnet ef migrations add Init --context OptimiserMySqlDbContext --output-dir Migrations/MySql

# Add the database and schema based on the connection string in /POC.Entities\DbContexts\OptimiserPgDbContextFactory.cs
dotnet ef database update --context OptimiserPgSqlDbContext
dotnet ef database update --context OptimiserMySqlDbContext
```

![Schema diagram](./Assets/schema-diagram.png)

1. a trial can have 0, 1 or many criteria
2. a criteria must have 1 trial
3. criteria must be unique by { trial_id, type }

## Entity Framework Strategies

## Entity Framework Concepts

This section focuses on Entity Framework's ability to work with multiple database providers beyond MS SQL Server, specifically MySQL and PostgreSQL.

Entity Framework, like most Object Relational Mapping (ORM) frameworks, is built around a few fundamental concepts:

- **Entity** — a class that represents a database table in code (e.g., `MyTable`)
- **Constraints** — rules applied to entity properties, such as:
  - Data type
  - Required / nullable
  - And more
- **Fluent API** — a programmatic, chainable approach to configuring entity mappings

Constraints can be applied via **data annotations**, the **Fluent API**, or a combination of both. Note that some constraints are only available through the Fluent API and cannot be expressed as data annotations.

**The Strategy:**

> Entity

The entity (MyTable) is static and will reflect the table called MyTable in the database. This table will have columns called Col1, Col2 and more however, the entity should be compatible across database tech stacks: MySql, PostgreSql, MS Sql, ... so it is important to not be too opinionated at this level.

> FluentAPI (class the inherits DbContext)

**OnModelCreating:** is the right place for Database-Specific constraints
OnModelCreating (inside the class that inherits DbContext) is where EF Core's Fluent API lives. Because it is configured per-context, it is naturally scoped to a specific database provider — MySQL, PostgreSQL, SQL Server, etc.

This makes it the ideal place to define the majority of your table and column constraints, since you can tailor the configuration precisely to what your target database supports.

> Bonus — Cross-Provider Portability via Data Annotations

EF Core also supports a degree of provider-agnostic mapping through data annotations, which it translates automatically based on whichever provider package is installed.

Take EntityBase as an example:

```c#
public abstract class EntityBase : IEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }
}
```
The annotations [Key] and [DatabaseGenerated(DatabaseGeneratedOption.Identity)] carry no provider-specific meaning on their own. EF Core delegates the translation to the active provider package:

| Provider | Package | Generate SQL |
|-|-|-|
| PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | SERIAL / GENERATED BY DEFAULT AS IDENTITY |
| MySQL | Pomelo.EntityFrameworkCore.MySql | INT AUTO_INCREMENT |

In this project both the MySQL and PostgreSQL contexts have their own concrete OnModelCreating implementations to handle anything the annotation layer cannot express — such as provider-specific enum types, index naming conventions, or custom column types. The data annotation on EntityBase.Id covers the common case; the Fluent API fills in the gaps.

## Entity Framework Translations

The most common question I hear when I talk about anything to do with EF is:

*It would be interesting to see how EF translates the query to TSQL*


```c#
return await _context.Trials
    .Include(p => p.Criterion)
    .ToListAsync();
```

```sql
--  PostgreSql
SELECT t.id, t.end_date, t.name, t.start_date, c.id, c.description, c.trial_id, c.type
      FROM trials AS t
      LEFT JOIN criterias AS c ON t.id = c.trial_id
      ORDER BY t.id

--  MySql
SELECT `t`.`Id`, `t`.`EndDate`, `t`.`Name`, `t`.`StartDate`, `c`.`id`, `c`.`Description`, `c`.`TrialId`, `c`.`Type`
      FROM `Trials` AS `t`
      LEFT JOIN `Criterion` AS `c` ON `t`.`Id` = `c`.`TrialId`
      ORDER BY `t`.`Id`

```

## Docker

Install both MySql and PostgreSql servers in your local Docker instance.

```bash
# MySql

## Pull latest MySQL
docker pull mysql:latest

## Run it
docker run --name mysql-dev-server \
  -e MYSQL_ROOT_PASSWORD=YourSecurePassword123 \
  -p 3306:3306 \
  -d mysql:latest

# PostgreSql

## Pull latest PostgreSQL
docker pull postgres:latest

## Run it
docker run --name postgres-dev-sever \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourSecurePassword123 \
  -p 5432:5432 \
  -d postgres:latest
```
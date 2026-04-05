# POC Database Entity Framework

I was recently engaged to migrate a project's database from MySQL to PostgreSQL, with Entity Framework already in place as the data access layer. My primary objective was to minimise disruption. That constraint led me to an idea: could I keep the entity models identical across both database providers, and push all provider specific configuration into the Fluent API of each respective DbContext?

The answer was yes. This is a simplified example of that approach, focusing on Entity Framework's ability to target multiple database providers simultaneously while preserving:
- Referential integrity
- Unique constraints
- Enum constraints


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
SELECT  "Id"
        , "Description" 
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
- POC.Entities\DbContexts\MySqlDbContextFactory.cs
- POC.Entities\DbContexts\PgDbContextFactory.cs

You will need .NET 9 and EF Core CLI 9.0.11 and run EF CLI commands:

In this case I already had EF Core 10 installed and I needed to downgrade since MySql is not yet compatible with .NET 10.

```bash
# If you are using a newer version Also, check if there is a global.json in the solution root if encountering any issues
dotnet tool uninstall --global dotnet-ef
dotnet tool install --global dotnet-ef --version 9.0.11


# install if not already installed or you have a previous version 
dotnet tool install --global dotnet-ef --version 9.0.11


# Create the database and schema - migrations already created
dotnet ef database update --context PgSqlDbContext
dotnet ef database update --context MySqlDbContext
```

Highly recommend you install https://dbeaver.io/ to read both database schemas without having to swap IDE's.

## Entity Framework CLI Commands

Generally there is only a single Fluent API in a project however, this composition was chosen to differentiate between MySql to PostgreSql.

In Terminal, PowerShell cd into: {your-directory}/poc-database-entity-framework/POC.Entities/

```bash
# Add a migration, select context and output directory
dotnet ef migrations add Init --context PgSqlDbContext --output-dir Migrations/PgSql

# Remove a migration
dotnet ef migrations remove --context PgSqlDbContext


dotnet ef migrations add Init --context MySqlDbContext --output-dir Migrations/MySql

# Add the database and schema based on the connection string in /POC.Entities\DbContexts\{Db-Provider}DbContextFactory.cs
dotnet ef database update --context PgSqlDbContext
dotnet ef database update --context MySqlDbContext
```

![Schema diagram](./Assets/schema-diagram.png)

1. a patient can have 0, 1 or many identifiers
2. a identifier must have 1 patient
3. identifier must be unique by { code, use }

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
return await _context.Patients
    .Include(p => p.Identifiers)
    .ToListAsync();
```

```sql
--  PostgreSql
 SELECT p.id, p.birth_date, p.name, i.id, i."Code", i.description, i.patient_id, i.use
      FROM patients AS p
      LEFT JOIN identifiers AS i ON p.id = i.patient_id
      ORDER BY p.id


--  MySql
SELECT `p`.`Id`, `p`.`BirthDate`, `p`.`Name`, `i`.`Id`, `i`.`Code`, `i`.`Description`, `i`.`PatientId`, `i`.`Use`
      FROM `Patients` AS `p`
      LEFT JOIN `Identifiers` AS `i` ON `p`.`Id` = `i`.`PatientId`
      ORDER BY `p`.`Id`


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
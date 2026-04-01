# POC Database Entity Framework

## Tech Stack

- Entity Framework
- PostgreSql
- .NET 6

## Conventions

> New for me: everything in lowercase unless you want to wrap double quotes around everything.

```sql
SELECT  *
FROM    "MyTableName"

SELECT 	id
		, description
		, type
		, trial_id
FROM    criterion;
```

> Double quotes for qualifying a string, not []

- MS SQL: []
- Pg SQL: ""

```sql

SELECT 	id
		, "description"
		, "type"
		, trial_id
FROM    criterion;
```



## EF CLI Commands

In Terminal, PowerShell cd into: {your-directory}/poc-database-entity-framework/POC.Entities/

```bash
# Add the database and schema based on the connection string in /POC.Entities\DbContexts\OptimiserPgDbContextFactory.cs
dotnet ef database update
```

![Schema diagram](./Assets/schema-diagram.png)

1. a trial can have 0, 1 or many criteria
2. a criteria must have 1 trial
3. criteria must be unique by { trial_id, type }

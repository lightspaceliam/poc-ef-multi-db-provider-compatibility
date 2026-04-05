using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using POC.Entities;
using POC.Entities.DbContexts;

namespace POC.Harness.Services;

public class PgSqlPatientEntityService(
    PgSqlDbContext context,
    ILogger<PgSqlPatientEntityService> logger)
{
    public async Task<Patient> CreateAsync(Patient entity)
    {
        try
        {
            context.Patients.Add(entity);
            await context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError("Transaction rolled back after error in {Operation}", nameof(CreateAsync));
            HandlePostgresException(nameof(CreateAsync), ex);
            throw;
        }
    }

    public async Task<List<Patient>> ReadAllAsync()
    {
        return await context.Patients
            .ToListAsync();
    }
    
    public async Task<List<Patient>> ReadAllWithIdentifiersAsync()
    {
        return await context.Patients
            .Include(p => p.Identifiers)
            .ToListAsync();
    }
    
    public async Task<Patient?> FindPatientByNamAsync(string name)
    {
        return await context.Patients
            .Include(p => p.Identifiers)
            .FirstOrDefaultAsync(p => p.Name == name);
    }
    
    public async Task<Identifier> CreateIdentifierAsync(Identifier entity)
    {
        try
        {
            context.Identifiers.Add(entity);
            await context.SaveChangesAsync();
            return entity;
        }
        catch (Exception ex)
        {
            logger.LogError("Transaction rolled back after error in {Operation}", nameof(CreateAsync));
            HandlePostgresException(nameof(CreateAsync), ex);
            throw;
        }
    }
    
    public void HandlePostgresException(string operation, Exception ex)
    {
        switch (ex)
        {
            // DbUpdateException wrapping a PostgresException — most common EF Core path
            case DbUpdateException { InnerException: PostgresException innerPgEx }:
                HandlePostgresErrorCode(operation, innerPgEx);
                break;

            // Bare PostgresException — e.g. from direct Npgsql commands
            case PostgresException pgEx:
                HandlePostgresErrorCode(operation, pgEx);
                break;

            // Network-level Npgsql failure (connection refused, host unreachable, SSL error)
            case NpgsqlException npgsqlEx:
                logger.LogError(npgsqlEx,
                    "Npgsql network-level error in {Operation}. IsTransient={IsTransient}",
                    operation, npgsqlEx.IsTransient);
                ExceptionDispatchInfo.Capture(npgsqlEx).Throw();
                break;

            // EF DbUpdateException with no inner PostgresException
            case DbUpdateException dbEx:
                logger.LogError(dbEx,
                    "EF DbUpdateException in {Operation} — no PostgreSQL error code available",
                    operation);
                ExceptionDispatchInfo.Capture(dbEx).Throw();
                break;

            // Cancellation token triggered by the caller
            case OperationCanceledException cancelEx:
                logger.LogWarning(cancelEx,
                    "{Operation} was cancelled by the caller",
                    operation);
                ExceptionDispatchInfo.Capture(cancelEx).Throw();
                break;

            // Command or connection timeout
            case TimeoutException timeoutEx:
                logger.LogError(timeoutEx,
                    "Timeout in {Operation}. Consider increasing CommandTimeout or checking server load",
                    operation);
                ExceptionDispatchInfo.Capture(timeoutEx).Throw();
                break;

            default:
                logger.LogError(ex,
                    "Unexpected error in {Operation}",
                    operation);
                ExceptionDispatchInfo.Capture(ex).Throw();
                break;
        }
    }

    private void HandlePostgresErrorCode(string operation, PostgresException ex)
    {
        switch (ex.SqlState)
        {
            // 23505 — unique_violation: duplicate PK or unique key
            case PostgresErrorCodes.UniqueViolation:
                logger.LogError(
                    "Duplicate key violation in {Operation}. " +
                    "Constraint={Constraint} Detail={Detail}",
                    operation, ex.ConstraintName, ex.Detail);
                throw new InvalidOperationException(
                    $"A record already exists (constraint: {ex.ConstraintName}). " +
                    "Consider upserting instead of inserting.", ex);

            // 23503 — foreign_key_violation: references a non-existent row
            case PostgresErrorCodes.ForeignKeyViolation:
                logger.LogError(
                    "Foreign key violation in {Operation}. " +
                    "Constraint={Constraint} Detail={Detail}",
                    operation, ex.ConstraintName, ex.Detail);
                throw new InvalidOperationException(
                    $"A record references a non-existent related row (constraint: {ex.ConstraintName}).", ex);

            // 23502 — not_null_violation: null sent to a NOT NULL column
            case PostgresErrorCodes.NotNullViolation:
                logger.LogError(
                    "Not-null violation in {Operation}. " +
                    "Column={Column} Table={Table}",
                    operation, ex.ColumnName, ex.TableName);
                throw new InvalidOperationException(
                    $"Column '{ex.ColumnName}' on table '{ex.TableName}' cannot be null.", ex);

            // 22001 — string_data_right_truncation: value too long for column
            case PostgresErrorCodes.StringDataRightTruncation:
                logger.LogError(
                    "String truncation in {Operation}. " +
                    "Column={Column} Detail={Detail}",
                    operation, ex.ColumnName, ex.Detail);
                throw new InvalidOperationException(
                    $"A value is too long for column '{ex.ColumnName}'.", ex);

            // 42P01 — undefined_table: table does not exist (migration not applied)
            case PostgresErrorCodes.UndefinedTable:
                logger.LogError(
                    "Table does not exist in {Operation}. Has the migration been applied? " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    "Target table does not exist. Ensure migrations have been applied before running the data migration.", ex);

            // 42703 — undefined_column: column missing (schema mismatch)
            case PostgresErrorCodes.UndefinedColumn:
                logger.LogError(
                    "Column does not exist in {Operation} — possible schema mismatch. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    $"Column not found in target schema: {ex.MessageText}", ex);

            // 28P01 — invalid_password: authentication failure
            case PostgresErrorCodes.InvalidPassword:
                logger.LogError(
                    "PostgreSQL authentication failed in {Operation}. " +
                    "Check connection string credentials. MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    "PostgreSQL authentication failed — verify the username and password in the connection string.", ex);

            // 3D000 — invalid_catalog_name: database does not exist
            case PostgresErrorCodes.InvalidCatalogName:
                logger.LogError(
                    "PostgreSQL database does not exist in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    $"PostgreSQL database does not exist: {ex.MessageText}", ex);

            // 53300 — too_many_connections: server connection pool exhausted
            case PostgresErrorCodes.TooManyConnections:
                logger.LogError(
                    "PostgreSQL server has too many connections in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    "PostgreSQL server has reached its connection limit. Try again later.", ex);

            // 55P03 — lock_not_available: lock wait timeout
            case PostgresErrorCodes.LockNotAvailable:
                logger.LogError(
                    "PostgreSQL lock not available in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new TimeoutException(
                    $"PostgreSQL lock wait timeout exceeded in {operation}. The target table may be locked.", ex);

            // 40P01 — deadlock_detected
            case PostgresErrorCodes.DeadlockDetected:
                logger.LogError(
                    "PostgreSQL deadlock detected in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    $"PostgreSQL deadlock detected in {operation}. Retry the operation.", ex);

            // 40001 — serialization_failure: concurrent transaction conflict
            case PostgresErrorCodes.SerializationFailure:
                logger.LogError(
                    "PostgreSQL serialization failure in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    $"PostgreSQL serialization failure in {operation} — concurrent transaction conflict. Retry the operation.", ex);

            // 57014 — query_canceled: statement timeout or manual cancel
            case PostgresErrorCodes.QueryCanceled:
                logger.LogError(
                    "PostgreSQL query cancelled in {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new OperationCanceledException(
                    $"The PostgreSQL operation was cancelled in {operation}.", ex);

            // 57P01 — admin_shutdown: server is shutting down
            case PostgresErrorCodes.AdminShutdown:
                logger.LogError(
                    "PostgreSQL server is shutting down during {Operation}. " +
                    "MessageText={Message}",
                    operation, ex.MessageText);
                throw new InvalidOperationException(
                    "PostgreSQL server is shutting down. Try again after the server restarts.", ex);

            default:
                logger.LogError(
                    "Unhandled PostgreSQL error in {Operation}. " +
                    "SqlState={SqlState} Severity={Severity} MessageText={Message}",
                    operation, ex.SqlState, ex.Severity, ex.MessageText);
                ExceptionDispatchInfo.Capture(ex).Throw();
                break;
        }
    }
}
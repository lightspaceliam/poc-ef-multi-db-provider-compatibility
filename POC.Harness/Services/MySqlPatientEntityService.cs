using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using POC.Entities;
using POC.Entities.DbContexts;

namespace POC.Harness.Services;

public class MySqlPatientEntityService(
    MySqlDbContext context,
    ILogger<MySqlPatientEntityService> logger)
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
            HandleMySqlException(nameof(CreateAsync), ex);
            throw; // unreachable — HandleMySqlException always throws, satisfies compiler
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
            HandleMySqlException(nameof(CreateIdentifierAsync), ex);
            throw; // unreachable — HandleMySqlException always throws, satisfies compiler
        }
    }

    /// <summary>
    /// Centrally handles all MySQL exceptions thrown during a database operation.
    /// Routes to <see cref="HandleMySqlErrorCode"/> for <see cref="MySqlException"/> cases
    /// and handles EF Core, cancellation, and timeout exceptions directly.
    /// </summary>
    public void HandleMySqlException(string operation, Exception ex)
    {
        switch (ex)
        {
            // MySqlException wrapped by EF Core's SaveChanges pipeline
            case DbUpdateException { InnerException: MySqlException innerMySqlEx }:
                HandleMySqlErrorCode(operation, innerMySqlEx);
                break;

            // Bare MySqlException — e.g. from direct ADO.NET or Pomelo commands
            case MySqlException mySqlEx:
                HandleMySqlErrorCode(operation, mySqlEx);
                break;

            // EF DbUpdateException with no inner MySqlException
            case DbUpdateException dbEx:
                logger.LogError(dbEx,
                    "EF DbUpdateException in {Operation} — no MySQL error code available",
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

    private void HandleMySqlErrorCode(string operation, MySqlException ex)
    {
        switch (ex.Number)
        {
            // 1045 — Access denied: wrong username or password
            case 1045:
                logger.LogError(
                    "MySQL access denied in {Operation}. " +
                    "Check connection string credentials. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    "MySQL access denied — verify the username and password in the connection string.", ex);

            // 1049 — Unknown database: database name does not exist
            case 1049:
                logger.LogError(
                    "MySQL database not found in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"MySQL database does not exist: {ex.Message}", ex);

            // 1146 — Table doesn't exist: migration not applied
            case 1146:
                logger.LogError(
                    "MySQL table or view does not exist in {Operation}. " +
                    "Has the migration been applied? " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Table not found in MySQL — ensure migrations have been applied: {ex.Message}", ex);

            // 1054 — Unknown column: schema mismatch between model and database
            case 1054:
                logger.LogError(
                    "MySQL unknown column in {Operation} — possible schema mismatch. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Column not found in MySQL schema: {ex.Message}", ex);

            // 1062 — Duplicate entry: unique key or primary key violation
            case 1062:
                logger.LogError(
                    "MySQL duplicate entry in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"A duplicate record already exists (unique constraint violation): {ex.Message}", ex);

            // 1216 — Foreign key constraint fails on INSERT or UPDATE
            // 1452 — Cannot add or update a child row: foreign key constraint fails
            case 1216:
            case 1452:
                logger.LogError(
                    "MySQL foreign key constraint violation in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Foreign key constraint failed — the referenced record does not exist: {ex.Message}", ex);

            // 1048 — Column cannot be null
            case 1048:
                logger.LogError(
                    "MySQL not-null constraint violation in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"A required column was null: {ex.Message}", ex);

            // 1406 — Data too long for column
            case 1406:
                logger.LogError(
                    "MySQL data too long for column in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"A value exceeds the maximum column length: {ex.Message}", ex);

            // 1042 — Can't get hostname: DNS resolution failure
            // 1130 — Host not allowed to connect
            case 1042:
            case 1130:
                logger.LogError(
                    "MySQL host connection error in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Cannot connect to MySQL host — check the server address and firewall rules: {ex.Message}", ex);

            // 1040 — Too many connections
            case 1040:
                logger.LogError(
                    "MySQL server has too many connections in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    "MySQL server has reached its connection limit. Try again later.", ex);

            // 1205 — Lock wait timeout exceeded
            case 1205:
                logger.LogError(
                    "MySQL lock wait timeout in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new TimeoutException(
                    $"MySQL lock wait timeout exceeded in {operation}. The table may be locked by another process.", ex);

            // 1213 — Deadlock found: transaction deadlock
            case 1213:
                logger.LogError(
                    "MySQL deadlock detected in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"MySQL deadlock detected in {operation}. Retry the operation.", ex);

            // 2002 — Can't connect to server through socket
            // 2003 — Can't connect to MySQL server on host
            case 2002:
            case 2003:
                logger.LogError(
                    "MySQL server unreachable in {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Cannot reach MySQL server — verify the host and port in the connection string: {ex.Message}", ex);

            // 2013 — Lost connection to server during query
            case 2013:
                logger.LogError(
                    "MySQL connection lost during {Operation}. " +
                    "ErrorCode={ErrorCode} Message={Message}",
                    operation, ex.Number, ex.Message);
                throw new InvalidOperationException(
                    $"Lost connection to MySQL server during {operation}. Check network stability and server timeout settings.", ex);

            default:
                logger.LogError(
                    "Unhandled MySQL error in {Operation}. " +
                    "ErrorCode={ErrorCode} SqlState={SqlState} Message={Message}",
                    operation, ex.Number, ex.SqlState, ex.Message);
                ExceptionDispatchInfo.Capture(ex).Throw();
                break;
        }
    }
}

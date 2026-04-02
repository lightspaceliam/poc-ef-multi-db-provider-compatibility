using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace POC.Entities.DbContexts;

/// <summary>
/// PostgreSql database provider.
/// </summary>
public class OptimiserPgSqlDbContext : DbContext
{
    // Register PostgreSQL native enum types once at startup so Npgsql
    // knows how to read/write them across all connections.
    // Also helps EF to add this to the {migration}.down
    static OptimiserPgSqlDbContext()
    {
#pragma warning disable CS0618 // GlobalTypeMapper is deprecated in Npgsql 8+ but still valid in 7.x
        NpgsqlConnection.GlobalTypeMapper.MapEnum<CriteriaTypes>("criteria_types");
#pragma warning restore CS0618
    }

    public OptimiserPgSqlDbContext(DbContextOptions<OptimiserPgSqlDbContext> options) : base(options)
    { }

    public OptimiserPgSqlDbContext()
    { }

    public DbSet<Trial> Trials { get; set; }
    public DbSet<Criteria> Criterion { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<CriteriaTypes>("public", "criteria_types");

        modelBuilder.Entity<Trial>(entity =>
        {
            //  Override the default name and case of database table and columns.
            entity.ToTable("trials");
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnName("name");
            entity.Property(e => e.StartDate)
                .HasColumnName("start_date");
            entity.Property(e => e.EndDate)
                .HasColumnName("end_date");
        });

        modelBuilder.Entity<Criteria>(entity =>
        {
            //  Override the default name and case of database table and columns.
            entity.ToTable("criterias");
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.HasKey(e => e.Id)
                .HasName("trial_pkey");
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasColumnType("criteria_types");
            entity.Property(e => e.TrialId)
                .HasColumnName("trial_id");

            // Enforce unique constraint: a Trial cannot have Criteria with duplicate values by { TrialId, Type }.
            entity.HasIndex(e => new { e.Type, e.TrialId })
                .IsUnique()
                .HasDatabaseName("unique_pgsql_criteria_trial_id_criteria_type");
        });

        base.OnModelCreating(modelBuilder);
    }
}

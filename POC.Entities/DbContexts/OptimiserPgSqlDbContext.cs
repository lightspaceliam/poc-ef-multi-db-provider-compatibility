using Microsoft.EntityFrameworkCore;

namespace POC.Entities.DbContexts;

/// <summary>
/// PostgreSql database provider.
/// </summary>
public class OptimiserPgSqlDbContext : DbContext
{
    public OptimiserPgSqlDbContext(DbContextOptions<OptimiserPgSqlDbContext> options) : base(options)
    { }

    public OptimiserPgSqlDbContext()
    { }

    public DbSet<Trial> Trials { get; set; }
    public DbSet<Criteria> Criterion { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            //  HasConversion<string>() stores the enum member name ("Inclusion", "Exclusion",
            //  "MainEvent") as varchar — no native PG enum type required, keeping the approach
            //  consistent with the MySql provider and entirely within OnModelCreating.
            entity.ToTable("criterias", t => t.HasCheckConstraint(
                "ck_criterias_type",
                "\"type\" IN ('Inclusion', 'Exclusion', 'MainEvent')"));
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.HasKey(e => e.Id)
                .HasName("trial_pkey");
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .HasMaxLength(50);
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

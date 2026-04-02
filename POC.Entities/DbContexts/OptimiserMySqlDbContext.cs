using Microsoft.EntityFrameworkCore;

namespace POC.Entities.DbContexts;

/// <summary>
/// MySql database provider.
/// </summary>
public class OptimiserMySqlDbContext : DbContext
{
    public OptimiserMySqlDbContext(DbContextOptions<OptimiserMySqlDbContext> options) : base(options)
    { }

    public OptimiserMySqlDbContext()
    { }

    public DbSet<Trial> Trials { get; set; }
    public DbSet<Criteria> Criterion { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Trial>(entity =>
        {
            //  In this case we will leave default case to demonstrate how OnModelCreating is bespoke to the database provider it's configured to.
            // //  Override the default name and case of database table and columns.
            // entity.ToTable("trials");
            // entity.Property(e => e.Id)
            //     .HasColumnName("id");
            // entity.Property(e => e.Name)
            //     .HasColumnName("name");
            // entity.Property(e => e.StartDate)
            //     .HasColumnName("start_date");
            // entity.Property(e => e.EndDate)
            //     .HasColumnName("end_date");
        });

        modelBuilder.Entity<Criteria>(entity =>
        {
            //  In this case we will leave default case to demonstrate how OnModelCreating is bespoke to the database provider it's configured to.
            // //  Override the default name and case of database table and columns.
            // entity.ToTable("criterion");
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.HasKey(e => e.Id)
                .HasName("trial_pkey");
            // entity.Property(e => e.Description).HasColumnName("description");

            //  MySQL does not support user-defined types. Inline ENUM enforces the same valid
            //  value constraint at the database level. HasConversion<string>() ensures EF stores
            //  the member name ("Inclusion") rather than the integer ordinal (0), which is what
            //  MySQL's ENUM column expects for string-keyed lookups.
            entity.Property(e => e.Type)
                //.HasColumnName("type")
                .HasConversion<string>()
                .HasColumnType("enum('Inclusion','Exclusion','Mainevent')");

            // entity.Property(e => e.TrialId)
            //     .HasColumnName("trial_id");

            // Enforce unique constraint: a Trial cannot have Criteria with duplicate values by { TrialId, Type }.
            entity.HasIndex(e => new { e.Type, e.TrialId })
                .IsUnique()
                .HasDatabaseName("unique_mysql_criteria_trial_id_criteria_type");
        });

        base.OnModelCreating(modelBuilder);
    }
}

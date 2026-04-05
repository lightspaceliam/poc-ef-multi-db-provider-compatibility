using Microsoft.EntityFrameworkCore;

namespace POC.Entities.DbContexts;

/// <summary>
/// MySql database provider.
/// </summary>
public class MySqlDbContext : DbContext
{
    public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options)
    { }

    public MySqlDbContext()
    { }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Identifier> Identifiers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            //  In this case we will leave default case to demonstrate how OnModelCreating is bespoke to the database provider it's configured to.
        });

        modelBuilder.Entity<Identifier>(entity =>
        {
            //  In this case we will leave default case to demonstrate how OnModelCreating is bespoke to the database provider it's configured to.
            //  MySQL does not support user-defined types. Inline ENUM enforces the same valid
            //  value constraint at the database level. HasConversion<string>() ensures EF stores
            //  the member name ("Inclusion") rather than the integer ordinal (0), which is what
            //  MySQL's ENUM column expects for string-keyed lookups.
            entity.Property(e => e.Use)
                .HasConversion<string>()
                .HasColumnType("enum('Official', 'Secondary', 'Temp', 'Usual', 'Old')");

            // Enforce unique constraint: a Patient cannot have identifier/s with duplicate values by { PatientId, Code, Use }.
            entity.HasIndex(e => new { e.Code, e.Use, e.PatientId })
                .IsUnique()
                .HasDatabaseName("unique_mysql_identifier_code_use_patient_id");
        });

        base.OnModelCreating(modelBuilder);
    }
}

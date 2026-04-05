using Microsoft.EntityFrameworkCore;

namespace POC.Entities.DbContexts;

/// <summary>
/// PostgreSql database provider.
/// </summary>
public class PgSqlDbContext : DbContext
{
    public PgSqlDbContext(DbContextOptions<PgSqlDbContext> options) : base(options)
    { }

    public PgSqlDbContext()
    { }

    public DbSet<Patient> Patients { get; set; }
    public DbSet<Identifier> Identifiers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Patient>(entity =>
        {
            //  Override the default name and case of database table and columns.
            entity.ToTable("patients");
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.HasKey(e => e.Id)
                .HasName("patients_pkey");
            entity.Property(e => e.Name)
                .HasColumnName("name");
            entity.Property(e => e.BirthDate)
                .HasColumnName("birth_date");
        });

        modelBuilder.Entity<Identifier>(entity =>
        {
            //  Override the default name and case of database table and columns.
            //  HasConversion<string>() stores the enum member name ('Official', 'Secondary', 'Temp', 'Usual', 'Old')
            //  as varchar — no native PG enum type required, keeping the approach
            //  consistent with the MySql provider and entirely within OnModelCreating.
            entity.ToTable("identifiers", t => t.HasCheckConstraint(
                "ck_identifiers_code_use",
                "\"use\" IN ('Official', 'Secondary', 'Temp', 'Usual', 'Old')"));
            entity.Property(e => e.Id)
                .HasColumnName("id");
            entity.HasKey(e => e.Id)
                .HasName("identifiers_pkey");
            entity.Property(e => e.Description)
                .HasColumnName("description");
            entity.Property(e => e.Use)
                .HasColumnName("use")
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.Property(e => e.PatientId)
                .HasColumnName("patient_id");

            // Enforce unique constraint: a Patient cannot have Identifier/s with duplicate values by { Code, Use }.
            entity.HasIndex(e => new { e.Code, e.Use, e.PatientId })
                .IsUnique()
                .HasDatabaseName("unique_pgsql_identifier_code_use_patient_id");
        });

        base.OnModelCreating(modelBuilder);
    }
}

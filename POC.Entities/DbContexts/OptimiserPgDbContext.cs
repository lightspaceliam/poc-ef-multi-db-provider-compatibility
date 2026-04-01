using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace POC.Entities.DbContexts;

public class OptimiserPgDbContext : DbContext
{
    // Register PostgreSQL native enum types once at startup so Npgsql
    // knows how to read/write them across all connections.
    // Also helps EF to add this to the {migration}.down
    static OptimiserPgDbContext()
    {
#pragma warning disable CS0618 // GlobalTypeMapper is deprecated in Npgsql 8+ but still valid in 7.x
        NpgsqlConnection.GlobalTypeMapper.MapEnum<CriteriaTypes>("criteria_types");
#pragma warning restore CS0618
    }
    public OptimiserPgDbContext(DbContextOptions options) : base(options)
    { }
    
    public OptimiserPgDbContext()
    { }
    
    public DbSet<Trial> Trials { get; set; }
    public DbSet<Criteria> Criterion { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<CriteriaTypes>("public", "criteria_types");
        
        //  Configure table constraint/s bespoke to Criteria. 
        modelBuilder.Entity<Criteria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("trial_pkey");
            
            //  Enforce Criteria.Type can only accept enum values from the Enum: CriteriaTypes
            entity.Property(e => e.Type)
                .HasColumnType("criteria_types");
            
            // Enforce unique constraint A Trial cannot have Criteria with duplicate values by { TrialId, Type }. 
            entity.HasIndex(e => new { e.Type, e.TrialId })
                .IsUnique()
                .HasDatabaseName("unique_criteria_trial_id_criteria_type");
        });
        base.OnModelCreating(modelBuilder);
    }
}
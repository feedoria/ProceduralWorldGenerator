using Microsoft.EntityFrameworkCore;

namespace TerrainSite;

// the connection to sqlite, every DbSet is a table
public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Run> Runs => Set<Run>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // saving the state as text ("Done") instead of a number, easier to read in the db
        b.Entity<Run>().Property(r => r.State).HasConversion<string>();
    }
}
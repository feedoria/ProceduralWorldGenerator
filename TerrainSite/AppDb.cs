using Microsoft.EntityFrameworkCore;

namespace TerrainSite;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Run> Runs => Set<Run>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // state saved as text ("Done"), readable when opening the db by hand
        b.Entity<Run>().Property(r => r.State).HasConversion<string>();
    }
}
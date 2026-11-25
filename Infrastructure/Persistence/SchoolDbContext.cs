using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

internal sealed class SchoolDbContext(DbContextOptions<SchoolDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolDbContext).Assembly);
        SchoolSeed.Configure(modelBuilder);
    }
}

using Microsoft.EntityFrameworkCore;
using PodcastApi.Domain;

namespace PodcastApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Guest> Guests => Set<Guest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Episode>()
            .HasIndex(e => e.AudioUrl)
            .IsUnique();

        modelBuilder.Entity<Guest>()
            .HasIndex(g => g.Name)
            .IsUnique();
    }
}
using Microsoft.EntityFrameworkCore;
using Tunora.Core.Domain.Entities;
using Tunora.Core.Domain.Interfaces;

namespace Tunora.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Instance> Instances => Set<Instance>();
    public DbSet<Channel> Channels => Set<Channel>();
    public DbSet<InstanceChannel> InstanceChannels => Set<InstanceChannel>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<PlaybackLog> PlaybackLogs => Set<PlaybackLog>();
    public DbSet<StripeEvent> StripeEvents => Set<StripeEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Seed the 5 default channels
        modelBuilder.Entity<Channel>().HasData(
            new Channel { Id = 1, Name = "Pop",        Description = "Upbeat popular music",      JamendoTag = "pop",        IconName = "music",     AccentColor = "#FF6B9D", IsActive = true },
            new Channel { Id = 2, Name = "Rock",       Description = "Classic and modern rock",   JamendoTag = "rock",       IconName = "guitar",    AccentColor = "#FF4444", IsActive = true },
            new Channel { Id = 3, Name = "Jazz",       Description = "Smooth jazz and blues",     JamendoTag = "jazz",       IconName = "music-2",   AccentColor = "#FFB347", IsActive = true },
            new Channel { Id = 4, Name = "Classical",  Description = "Orchestral and piano",      JamendoTag = "classical",  IconName = "violin",    AccentColor = "#9B59B6", IsActive = true },
            new Channel { Id = 5, Name = "Electronic", Description = "Ambient and electronic",    JamendoTag = "electronic", IconName = "zap",       AccentColor = "#1DB954", IsActive = true }
        );
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;

            if (entry.State is EntityState.Added or EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

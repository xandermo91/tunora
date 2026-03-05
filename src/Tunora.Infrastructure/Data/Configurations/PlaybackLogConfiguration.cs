using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Data.Configurations;

public class PlaybackLogConfiguration : IEntityTypeConfiguration<PlaybackLog>
{
    public void Configure(EntityTypeBuilder<PlaybackLog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TrackId).HasMaxLength(100);
        builder.Property(x => x.TrackTitle).HasMaxLength(300);
        builder.Property(x => x.ArtistName).HasMaxLength(200);

        builder.HasOne(x => x.Instance)
               .WithMany(x => x.PlaybackLogs)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Channel)
               .WithMany()
               .HasForeignKey(x => x.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

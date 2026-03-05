using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Data.Configurations;

public class InstanceChannelConfiguration : IEntityTypeConfiguration<InstanceChannel>
{
    public void Configure(EntityTypeBuilder<InstanceChannel> builder)
    {
        builder.HasKey(x => new { x.InstanceId, x.ChannelId });

        builder.HasOne(x => x.Instance)
               .WithMany(x => x.InstanceChannels)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Channel)
               .WithMany(x => x.InstanceChannels)
               .HasForeignKey(x => x.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

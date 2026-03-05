using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Data.Configurations;

public class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    public void Configure(EntityTypeBuilder<Instance> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(500);
        builder.Property(x => x.ConnectionKey).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.ConnectionKey).IsUnique();
        builder.Property(x => x.CurrentTrackTitle).HasMaxLength(300);
        builder.Property(x => x.CurrentTrackArtist).HasMaxLength(200);

        builder.HasOne(x => x.Company)
               .WithMany(x => x.Instances)
               .HasForeignKey(x => x.CompanyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ActiveChannel)
               .WithMany()
               .HasForeignKey(x => x.ActiveChannelId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

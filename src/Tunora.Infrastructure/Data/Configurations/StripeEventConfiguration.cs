using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Data.Configurations;

public class StripeEventConfiguration : IEntityTypeConfiguration<StripeEvent>
{
    public void Configure(EntityTypeBuilder<StripeEvent> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StripeEventId).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.StripeEventId).IsUnique();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
    }
}

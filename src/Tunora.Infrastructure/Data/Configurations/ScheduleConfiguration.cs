using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tunora.Core.Domain.Entities;

namespace Tunora.Infrastructure.Data.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.QuartzJobKey).HasMaxLength(200);

        // Store DaysOfWeek array as a JSON string in SQL Server
        builder.Property(x => x.DaysOfWeek)
               .HasConversion(
                   v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                   v => JsonSerializer.Deserialize<DayOfWeek[]>(v, (JsonSerializerOptions?)null) ?? Array.Empty<DayOfWeek>()
               )
               .HasColumnType("nvarchar(100)");

        builder.Property(x => x.StartTime).HasColumnType("time");
        builder.Property(x => x.EndTime).HasColumnType("time");

        builder.HasOne(x => x.Instance)
               .WithMany(x => x.Schedules)
               .HasForeignKey(x => x.InstanceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Channel)
               .WithMany(x => x.Schedules)
               .HasForeignKey(x => x.ChannelId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

using System.ComponentModel.DataAnnotations;

namespace Tunora.API.DTOs.Schedules;

public record ScheduleDto(
    int Id,
    string Name,
    int InstanceId,
    int ChannelId,
    string ChannelName,
    string ChannelAccentColor,
    int[] DaysOfWeek,
    string StartTime,
    string EndTime,
    bool IsActive,
    DateTime CreatedAt);

public record CreateScheduleDto(
    int ChannelId,
    int[] DaysOfWeek,
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Must be HH:mm format.")] string StartTime,
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Must be HH:mm format.")] string EndTime);

public record UpdateScheduleDto(
    int ChannelId,
    int[] DaysOfWeek,
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Must be HH:mm format.")] string StartTime,
    [RegularExpression(@"^\d{2}:\d{2}$", ErrorMessage = "Must be HH:mm format.")] string EndTime,
    bool IsActive);

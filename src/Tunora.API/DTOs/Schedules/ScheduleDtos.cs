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
    string StartTime,
    string EndTime);

public record UpdateScheduleDto(
    int ChannelId,
    int[] DaysOfWeek,
    string StartTime,
    string EndTime,
    bool IsActive);

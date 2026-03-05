namespace Tunora.Infrastructure.Models;

public record CreateScheduleRequest(
    string Name,
    int ChannelId,
    DayOfWeek[] DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public record UpdateScheduleRequest(
    string Name,
    int ChannelId,
    DayOfWeek[] DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

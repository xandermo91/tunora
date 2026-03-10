namespace Tunora.Infrastructure.Models;

public record CreateScheduleRequest(
    int ChannelId,
    DayOfWeek[] DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public record UpdateScheduleRequest(
    int ChannelId,
    DayOfWeek[] DaysOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    bool IsActive);

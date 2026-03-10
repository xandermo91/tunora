using Microsoft.EntityFrameworkCore;
using Quartz;
using Tunora.Core.Domain.Entities;
using Tunora.Core.Exceptions;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Models;

namespace Tunora.Infrastructure.Services;

public class ScheduleService(ApplicationDbContext db, ISchedulerFactory schedulerFactory) : IScheduleService
{
    public async Task<List<Schedule>> GetByInstanceAsync(int instanceId, int companyId, CancellationToken ct = default) =>
        await db.Schedules
            .AsNoTracking()
            .Include(s => s.Channel)
            .Where(s => s.InstanceId == instanceId && s.Instance.CompanyId == companyId)
            .OrderBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task<List<Schedule>> GetByCompanyAsync(int companyId, CancellationToken ct = default) =>
        await db.Schedules
            .AsNoTracking()
            .Include(s => s.Channel)
            .Include(s => s.Instance)
            .Where(s => s.Instance.CompanyId == companyId && s.Instance.IsActive)
            .OrderBy(s => s.Instance.Name).ThenBy(s => s.StartTime)
            .ToListAsync(ct);

    public async Task<List<Schedule>> GetAllActiveAsync(CancellationToken ct = default)
    {
        // DaysOfWeek is stored as JSON — filter its length in memory after the SQL query
        var all = await db.Schedules
            .AsNoTracking()
            .Include(s => s.Channel)
            .Where(s => s.IsActive)
            .ToListAsync(ct);
        return all.Where(s => s.DaysOfWeek.Length > 0).ToList();
    }

    public async Task<Schedule> CreateAsync(int instanceId, int companyId, CreateScheduleRequest req, CancellationToken ct = default)
    {
        var instanceExists = await db.Instances
            .AnyAsync(i => i.Id == instanceId && i.CompanyId == companyId && i.IsActive, ct);
        if (!instanceExists) throw new NotFoundException($"Instance {instanceId} not found.");

        var channel = await db.Channels.FirstOrDefaultAsync(c => c.Id == req.ChannelId && c.IsActive, ct)
            ?? throw new NotFoundException($"Channel {req.ChannelId} not found.");

        await ValidateNoOverlapAsync(instanceId, null, req.DaysOfWeek, req.StartTime, req.EndTime, ct);

        var schedule = new Schedule
        {
            InstanceId = instanceId,
            ChannelId  = req.ChannelId,
            Name       = $"{channel.Name} {req.StartTime:HH\\:mm}–{req.EndTime:HH\\:mm}",
            DaysOfWeek = req.DaysOfWeek,
            StartTime  = req.StartTime,
            EndTime    = req.EndTime,
            IsActive   = true,
        };

        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(ct);

        schedule.Channel = channel;
        await RegisterWithQuartzAsync(schedule, ct);
        return schedule;
    }

    public async Task<Schedule> UpdateAsync(int id, int companyId, UpdateScheduleRequest req, CancellationToken ct = default)
    {
        var schedule = await db.Schedules
            .Include(s => s.Instance)
            .Include(s => s.Channel)
            .FirstOrDefaultAsync(s => s.Id == id && s.Instance.CompanyId == companyId, ct)
            ?? throw new NotFoundException($"Schedule {id} not found.");

        var channel = req.ChannelId != schedule.Channel.Id
            ? (await db.Channels.FindAsync([req.ChannelId], ct) ?? throw new NotFoundException($"Channel {req.ChannelId} not found."))
            : schedule.Channel;

        await ValidateNoOverlapAsync(schedule.InstanceId, id, req.DaysOfWeek, req.StartTime, req.EndTime, ct);
        await UnregisterFromQuartzAsync(id, ct);

        schedule.Name       = $"{channel.Name} {req.StartTime:HH\\:mm}–{req.EndTime:HH\\:mm}";
        schedule.ChannelId  = req.ChannelId;
        schedule.DaysOfWeek = req.DaysOfWeek;
        schedule.StartTime  = req.StartTime;
        schedule.EndTime    = req.EndTime;
        schedule.IsActive   = req.IsActive;
        schedule.Channel    = channel;

        await db.SaveChangesAsync(ct);

        if (req.IsActive && req.DaysOfWeek.Length > 0)
            await RegisterWithQuartzAsync(schedule, ct);

        return schedule;
    }

    public async Task DeleteAsync(int id, int companyId, CancellationToken ct = default)
    {
        var schedule = await db.Schedules
            .Include(s => s.Instance)
            .FirstOrDefaultAsync(s => s.Id == id && s.Instance.CompanyId == companyId, ct)
            ?? throw new NotFoundException($"Schedule {id} not found.");

        await UnregisterFromQuartzAsync(id, ct);
        db.Schedules.Remove(schedule);
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterWithQuartzAsync(Schedule schedule, CancellationToken ct = default)
    {
        if (!schedule.IsActive || schedule.DaysOfWeek.Length == 0) return;

        var scheduler = await schedulerFactory.GetScheduler(ct);

        var startTrigger = TriggerBuilder.Create()
            .WithIdentity($"{schedule.Id}-start", "schedules")
            .WithCronSchedule(BuildCron(schedule.StartTime, schedule.DaysOfWeek))
            .UsingJobData("instanceId", schedule.InstanceId)
            .UsingJobData("channelId",  schedule.ChannelId)
            .UsingJobData("action",     "Play")
            .ForJob("channel-switch", "tunora")
            .Build();

        var stopTrigger = TriggerBuilder.Create()
            .WithIdentity($"{schedule.Id}-stop", "schedules")
            .WithCronSchedule(BuildCron(schedule.EndTime, schedule.DaysOfWeek))
            .UsingJobData("instanceId", schedule.InstanceId)
            .UsingJobData("channelId",  0)
            .UsingJobData("action",     "Stop")
            .ForJob("channel-switch", "tunora")
            .Build();

        await scheduler.ScheduleJob(startTrigger, ct);
        await scheduler.ScheduleJob(stopTrigger,  ct);
    }

    private async Task ValidateNoOverlapAsync(
        int instanceId, int? excludeId,
        DayOfWeek[] days, TimeOnly start, TimeOnly end,
        CancellationToken ct)
    {
        // DaysOfWeek is JSON — load in memory, then filter
        var existing = await db.Schedules
            .AsNoTracking()
            .Where(s => s.InstanceId == instanceId && s.IsActive &&
                        (excludeId == null || s.Id != excludeId))
            .ToListAsync(ct);

        foreach (var s in existing)
        {
            if (!s.DaysOfWeek.Any(d => days.Contains(d))) continue;
            if (TimeRangesOverlap(start, end, s.StartTime, s.EndTime))
                throw new ValidationException("This time slot overlaps with an existing schedule.");
        }
    }

    private static bool TimeRangesOverlap(TimeOnly s1, TimeOnly e1, TimeOnly s2, TimeOnly e2)
    {
        int a0 = s1.Hour * 60 + s1.Minute, a1 = e1.Hour * 60 + e1.Minute;
        int b0 = s2.Hour * 60 + s2.Minute, b1 = e2.Hour * 60 + e2.Minute;
        if (a1 <= a0) a1 += 1440;   // midnight crossing
        if (b1 <= b0) b1 += 1440;
        return (a0 < b1 && b0 < a1) || (a0 < b1 + 1440 && b0 + 1440 < a1);
    }

    private async Task UnregisterFromQuartzAsync(int scheduleId, CancellationToken ct = default)
    {
        var scheduler = await schedulerFactory.GetScheduler(ct);
        await scheduler.UnscheduleJob(new TriggerKey($"{scheduleId}-start", "schedules"), ct);
        await scheduler.UnscheduleJob(new TriggerKey($"{scheduleId}-stop",  "schedules"), ct);
    }

    private static string BuildCron(TimeOnly time, DayOfWeek[] days)
    {
        var dayStr = string.Join(",", days.Select(d => d switch
        {
            DayOfWeek.Sunday    => "SUN",
            DayOfWeek.Monday    => "MON",
            DayOfWeek.Tuesday   => "TUE",
            DayOfWeek.Wednesday => "WED",
            DayOfWeek.Thursday  => "THU",
            DayOfWeek.Friday    => "FRI",
            DayOfWeek.Saturday  => "SAT",
            _                   => throw new ArgumentOutOfRangeException(nameof(days)),
        }));
        return $"0 {time.Minute} {time.Hour} ? * {dayStr}";
    }
}

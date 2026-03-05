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

        var schedule = new Schedule
        {
            InstanceId = instanceId,
            ChannelId  = req.ChannelId,
            Name       = req.Name,
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

        await UnregisterFromQuartzAsync(id, ct);

        schedule.Name       = req.Name;
        schedule.ChannelId  = req.ChannelId;
        schedule.DaysOfWeek = req.DaysOfWeek;
        schedule.StartTime  = req.StartTime;
        schedule.EndTime    = req.EndTime;
        schedule.IsActive   = req.IsActive;

        if (req.ChannelId != schedule.Channel.Id)
            schedule.Channel = (await db.Channels.FindAsync([req.ChannelId], ct))!;

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

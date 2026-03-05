using Microsoft.AspNetCore.SignalR;
using Quartz;
using Tunora.API.Hubs;

namespace Tunora.API.Jobs;

/// <summary>Fires on a cron schedule and sends a play or stop command to an in-store player via SignalR.</summary>
[DisallowConcurrentExecution]
public class ChannelSwitchJob(IHubContext<PlaybackHub> hubContext) : IJob
{
    public static readonly JobKey Key = new("channel-switch", "tunora");

    public async Task Execute(IJobExecutionContext context)
    {
        var map = context.MergedJobDataMap;
        var instanceId = map.GetInt("instanceId");
        var channelId  = map.GetIntValue("channelId");
        var action     = map.GetString("action")!;

        var command = new PlaybackCommand(action, action == "Play" ? channelId : (int?)null);
        await hubContext.Clients
            .Group($"instance-{instanceId}")
            .SendAsync("ReceiveCommand", command);
    }
}

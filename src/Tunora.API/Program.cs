using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using Tunora.API.Hubs;
using Tunora.API.Jobs;
using Tunora.API.Middleware;
using Tunora.Infrastructure.Data;
using Tunora.Infrastructure.Options;
using Tunora.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Options ─────────────────────────────────────────────────────────────────
builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration("JwtOptions")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<JamendoOptions>(builder.Configuration.GetSection("JamendoOptions"));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("StripeOptions"));
builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection("ClaudeOptions"));

// ── Authentication / JWT ────────────────────────────────────────────────────
var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>()!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep short claim names (role, sub, etc.) as-is
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        // Allow SignalR to send JWT via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Rate Limiting ────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit          = 10;
        o.Window               = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });
    // Advisor rate limit is partitioned per company so one tenant can't exhaust the budget for others
    opts.AddPolicy("advisor", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirstValue("companyId") ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 5,
                Window               = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            }));
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Application Services ────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITierLimitService, TierLimitService>();
builder.Services.AddScoped<IInstanceService, InstanceService>();
builder.Services.AddScoped<IKioskService, KioskService>();
builder.Services.AddScoped<IPlaybackService, PlaybackService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// ── Quartz Scheduler ─────────────────────────────────────────────────────────
builder.Services.AddQuartz(q =>
{
    // Register the single job type; triggers are added dynamically per schedule
    q.AddJob<ChannelSwitchJob>(j => j.WithIdentity(ChannelSwitchJob.Key).StoreDurably());
});
builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);
// Runs after QuartzHostedService — registration order guarantees scheduler is started
builder.Services.AddHostedService<ScheduleLoaderService>();

// ── Jamendo HTTP Client ──────────────────────────────────────────────────────
builder.Services.AddHttpClient<JamendoClient>((sp, client) =>
{
    var opts = builder.Configuration.GetSection("JamendoOptions").Get<JamendoOptions>()!;
    client.BaseAddress = new Uri(opts.BaseUrl);
});

// ── Claude AI Advisor ────────────────────────────────────────────────────────
// Single registration: typed client wires IClaudeAdvisorService → ClaudeAdvisorService
// with the pre-configured HttpClient (x-api-key header set at startup).
builder.Services.AddHttpClient<IClaudeAdvisorService, ClaudeAdvisorService>((sp, client) =>
{
    var apiKey = builder.Configuration["ClaudeOptions:ApiKey"] ?? "";
    client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
});

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── CORS ────────────────────────────────────────────────────────────────────
// Supports two formats:
//   1. CORS_ORIGINS env var — comma-separated list (simplest for Azure)
//   2. CorsOrigins array in config — CorsOrigins__0, CorsOrigins__1, ...
var corsOriginsEnv = builder.Configuration["CORS_ORIGINS"];
string[] corsOrigins;
if (!string.IsNullOrWhiteSpace(corsOriginsEnv))
{
    corsOrigins = corsOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
else
{
    corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
        ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5175"];
}

var startupLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
startupLogger.LogInformation("CORS origins configured: {Origins}", string.Join(", ", corsOrigins));

builder.Services.AddCors(options =>
{
    options.AddPolicy("TunoraCors", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Controllers + Swagger ───────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();   // JWT security UI added in Phase 2 once Swashbuckle compat confirmed

// ── Build ────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("TunoraCors");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.MapHub<PlaybackHub>("/hubs/playback");

app.Run();

// Make Program accessible to WebApplicationFactory in tests
public partial class Program { }

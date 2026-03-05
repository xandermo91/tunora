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

// ── Rate Limiting — auth endpoints only ─────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("auth", o =>
    {
        o.PermitLimit         = 10;
        o.Window              = TimeSpan.FromMinutes(1);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit          = 0;
    });
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

// ── SignalR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── CORS ────────────────────────────────────────────────────────────────────
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:5174", "http://localhost:5175"];

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

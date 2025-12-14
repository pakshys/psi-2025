using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using backend.Extensions;
using backend.Database;
using backend.Models;
using backend.Services;
using backend.Hubs;
using backend.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

// Adding CORS services
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("http://localhost:5173") // React server origin
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()); // for SignalR !
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure authentication and authorization
builder.Services.AddIdentity<User, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = ctx =>
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Register PartyRoomService for dependency injection
builder.Services.AddScoped<IPartyRoomService, PartyRoomService>();

// Register UserProfileService
builder.Services.AddScoped<IUserProfileService, UserProfileService>();

// Register FriendshipService
builder.Services.AddScoped<IFriendshipService, FriendshipService>();

// RegisterTrackQueueService
builder.Services.AddScoped<ITrackQueueService, TrackQueueService>();

builder.Services.AddSingleton<IRoomStateService, RoomStateService>();
builder.Services.AddSingleton<IVoteService, VoteService>();

// Bind the YouTube settings
builder.Services.Configure<YouTubeSettings>(builder.Configuration.GetSection("YouTubeApi"));

// Register SignalR for real-time functionalities
builder.Services.AddSignalR(options =>
{
  options.EnableDetailedErrors = true;
}).AddHubOptions<PartyRoomHub>(options =>
{
  options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
});

builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
  
    app.ApplyMigrations();  
}

// Add exception handling middleware (MUST be early in pipeline)
app.UseMiddleware<ExceptionHandlingMiddleware>();

//app.UseHttpsRedirection(); //(BAD HANDSHAKE HTTP <-> HTTPS issue)

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR PartyRoomHub
app.MapHub<PartyRoomHub>("/hubs/partyroom");

try
{
    Log.Information("Starting web application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
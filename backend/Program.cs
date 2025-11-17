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
builder.Services.AddIdentity(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores()
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

builder.Services.AddDbContext(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Register PartyRoomService for dependency injection
builder.Services.AddScoped();

// Register UserProfileService
builder.Services.AddScoped();

// Register FriendshipService
builder.Services.AddScoped();

// RegisterTrackQueueService
builder.Services.AddScoped();

builder.Services.AddSingleton();
builder.Services.AddSingleton();

// Register SignalR for real-time functionalities
builder.Services.AddSignalR(options =>
{
  options.EnableDetailedErrors = true;
}).AddHubOptions(options =>
{
  options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
});

builder.Services.AddSingleton();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
  
    app.ApplyMigrations();  
}

// Add exception handling middleware (MUST be early in pipeline)
app.UseMiddleware();

//app.UseHttpsRedirection(); //(BAD HANDSHAKE HTTP  HTTPS issue)

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR PartyRoomHub
app.MapHub("/hubs/partyroom");

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
```

---

## Step 5: Update appsettings.json

Add Serilog configuration:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Port=5432;Database=identity;Username=postgres;Password=postgres"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information", 
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## Step 6: Update PartyRoomService.cs

Replace key methods to throw `NotFoundException` and add logging:

```csharp
using backend.Exceptions;
using backend.Database;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PartyRoomService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger _logger;

  public PartyRoomService(ApplicationDbContext context, ILogger logger)
  {
    _context = context;
    _logger = logger;
  }

  // Get all rooms
  public async Task<List> GetAllAsync()
  {
    _logger.LogInformation("Fetching all party rooms");
    return await _context.PartyRooms.ToListAsync();
  }

  // Get room by ID - throws NotFoundException
  public async Task GetByIdAsync(int id)
  {
    _logger.LogInformation("Fetching party room with ID: {RoomId}", id);
    
    var room = await _context.PartyRooms.FindAsync(id);
    
    if (room == null)
    {
      _logger.LogWarning("Party room with ID {RoomId} not found", id);
      throw new NotFoundException($"Party room with ID {id} not found.");
    }
    
    return room;
  }

  // Create a new room
  public async Task CreateAsync(string name, int capacity = 10, bool isPrivate = false)
  {
    if (string.IsNullOrWhiteSpace(name))
      throw new ArgumentException("Party room name cannot be empty.");

    if (capacity <= 0)
      throw new ArgumentException("Party room capacity must be greater than zero.");

    var partyRoom = new PartyRoom
    {
      Name = name,
      Capacity = capacity,
      IsPrivate = isPrivate
    };

    _context.PartyRooms.Add(partyRoom);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Created party room: {RoomName} (ID: {RoomId})", name, partyRoom.Id);
    return partyRoom;
  }

  // Join a room
  public async Task JoinAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount >= room.Capacity)
      throw new InvalidOperationException("Party room is full.");
    
    _logger.LogInformation("User joined party room: {RoomId}", id);
  }

  // Leave a room
  public async Task LeaveAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    var currentCount = room.Members?.Count ?? 0;
    if (currentCount <= 0)
      throw new InvalidOperationException("Party room is already empty.");
    
    _logger.LogInformation("User left party room: {RoomId}", id);
  }

  // Update a room
  public async Task UpdateAsync(PartyRoom updatedRoom)
  {
    var existingRoom = await GetByIdAsync(updatedRoom.Id); // Will throw NotFoundException if not found

    if (string.IsNullOrWhiteSpace(updatedRoom.Name))
      throw new ArgumentException("Party room name cannot be empty.");

    if (updatedRoom.Capacity <= 0)
      throw new ArgumentException("Party room capacity must be greater than zero.");

    var currentCount = existingRoom.Members?.Count ?? 0;
    if (updatedRoom.Capacity < currentCount)
      throw new InvalidOperationException("New capacity cannot be less than current guests count.");

    existingRoom.Name = updatedRoom.Name;
    existingRoom.Capacity = updatedRoom.Capacity;
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Updated party room: {RoomId}", updatedRoom.Id);
  }

  // Delete a room
  public async Task DeleteAsync(int id)
  {
    var room = await GetByIdAsync(id); // Will throw NotFoundException if not found

    _context.PartyRooms.Remove(room);
    await _context.SaveChangesAsync();
    
    _logger.LogInformation("Deleted party room: {RoomId}", id);
  }
}
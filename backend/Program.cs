using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using backend.Extensions;
using backend.Database;
using backend.Models;
using backend.Services;
using backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddAuthorization();

//builder.Services.AddIdentityCore<User>(options =>
//{
//    options.SignIn.RequireConfirmedAccount = false;
//})
//.AddEntityFrameworkStores<ApplicationDbContext>()
//.AddSignInManager()
//.AddApiEndpoints();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));


// Register PartyRoomService for dependency injection
builder.Services.AddScoped<PartyRoomService>();

// Register UserProfileService
builder.Services.AddScoped<UserProfileService>();

// Register FriendshipService
builder.Services.AddScoped<FriendshipService>();

// RegisterTrackQueueService
builder.Services.AddScoped<TrackQueueService>();

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

//app.UseHttpsRedirection(); //(BAD HANDSHAKE HTTP <-> HTTPS issue)

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR PartyRoomHub
app.MapHub<PartyRoomHub>("/hubs/partyroom");

app.Run();

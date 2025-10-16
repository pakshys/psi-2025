using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            .AllowCredentials());
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure authentication and authorization
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddIdentityCore<User>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddApiEndpoints();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// Register PartyRoomService for dependency injection
builder.Services.AddScoped<PartyRoomService>();

// Register UserProfileService
builder.Services.AddScoped<UserProfileService>();

// Register FriendshipService
builder.Services.AddScoped<FriendshipService>();

// Register SignalR for real-time functionalities
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.ApplyMigrations();  
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseCors("AllowFrontend");

app.MapControllers();

// Map SignalR PartyRoomHub
app.MapHub<PartyRoomHub>("/hubs/partyroom");

app.Run();

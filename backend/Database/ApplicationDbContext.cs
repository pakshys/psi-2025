using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Database;

public class ApplicationDbContext : IdentityDbContext<User>
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSet for PartyRoom entity
    public DbSet<PartyRoom> PartyRooms => Set<PartyRoom>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Friendship> Friendships => Set<Friendship>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Default schema is "app"
        builder.HasDefaultSchema("app");

        // Moving tables related to identity to "identity" schema
        builder.Entity<User>().ToTable("Users", "identity");
        builder.Entity<IdentityRole>().ToTable("Roles", "identity");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles", "identity");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims", "identity");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins", "identity");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims", "identity");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens", "identity");

        // === Application entities (default schema "app") ===
        // UserProfile configuration: one profile per user
        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");                  // "app"."UserProfiles"
            entity.HasIndex(p => p.UserId).IsUnique();       // Ensure one profile per user
            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Friendship configuration: bidirectional relationship between users
        builder.Entity<Friendship>(entity =>
        {
            entity.ToTable("Friendships");                   // "app"."Friendships"

            // Unique pair in one direction (Requester -> Addressee)
            entity.HasIndex(f => new { f.RequesterId, f.AddresseeId })
                  .IsUnique();

            // Additional index for reverse direction (Addressee -> Requester)
            entity.HasIndex(f => new { f.AddresseeId, f.RequesterId });

            entity.HasOne(f => f.Requester)
                  .WithMany()
                  .HasForeignKey(f => f.RequesterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(f => f.Addressee)
                  .WithMany()
                  .HasForeignKey(f => f.AddresseeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

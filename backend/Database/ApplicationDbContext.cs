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
    }
}

using backend.Database;
using Microsoft.EntityFrameworkCore;

namespace backend.Helpers;

public static class MigrationHelpers
{
	public static void ApplyMigrations(this IApplicationBuilder app)
	{
		using IServiceScope scope = app.ApplicationServices.CreateScope();

		using ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

		context.Database.Migrate();
	}
}
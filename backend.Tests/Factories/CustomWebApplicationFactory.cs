using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using backend.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace backend.Tests.Factories
{
  public class TestFactory<TProgram> : WebApplicationFactory<TProgram>
      where TProgram : class
  {
    private readonly string _dbName;
    private readonly string _environment;
    private readonly List<(Type serviceType, object instance)> _overrides = new();

    public TestFactory(string environment = "Testing")
    {
      _environment = environment;
      _dbName = Guid.NewGuid().ToString();
    }

    public void ConfigureService<TService>(TService instance)
        where TService : class
    {
      _overrides.Add((typeof(TService), instance!));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
      builder.UseSetting("environment", _environment);

      builder.ConfigureServices(services =>
      {
        var dbDescriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

        if (dbDescriptor != null)
          services.Remove(dbDescriptor);

        services.AddDbContext<ApplicationDbContext>(options =>
        {
          options.UseInMemoryDatabase(_dbName);
        });

        foreach (var (serviceType, instance) in _overrides)
        {
          var existing = services.Where(s => s.ServiceType == serviceType).ToList();

          foreach (var service in existing)
            services.Remove(service);

          services.AddSingleton(serviceType, instance);
        }

        services.AddAuthentication(options =>
        {
          options.DefaultAuthenticateScheme = FakeAuthHandler.SchemeName;
          options.DefaultChallengeScheme = FakeAuthHandler.SchemeName;
        })
        .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(
            FakeAuthHandler.SchemeName,
            options => { }
        );

        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
      });
    }
  }
}

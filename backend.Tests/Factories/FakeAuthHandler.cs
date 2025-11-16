using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using backend.Models;
using Microsoft.AspNetCore.Identity;

namespace backend.Tests.Factories
{
  public class FakeAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
  {
    public const string SchemeName = "Test";

    public FakeAuthHandler(
      IOptionsMonitor<AuthenticationSchemeOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder)
      : base(options, logger, encoder)
    { }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
      var header = Request.Headers["Authorization"].ToString();
      if (string.IsNullOrEmpty(header))
        return AuthenticateResult.Fail("No Authorization header provided");

      var username = header.Replace(SchemeName + " ", "").Trim();
      if (string.IsNullOrEmpty(username))
        return AuthenticateResult.Fail("No username provided");

      var userManager = Context.RequestServices.GetService<UserManager<User>>();
      User? user = null;
      if (userManager != null)
      {
        user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(username);
      }

      if (user == null)
        return AuthenticateResult.Fail("No matching identity user found");

      var claims = new[]
      {
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, username),
        new Claim(ClaimTypes.Email, user.Email ?? username)
      };

      var identity = new ClaimsIdentity(claims, SchemeName);
      var principal = new ClaimsPrincipal(identity);
      var ticket = new AuthenticationTicket(principal, SchemeName);

      return AuthenticateResult.Success(ticket);
    }
  }
}
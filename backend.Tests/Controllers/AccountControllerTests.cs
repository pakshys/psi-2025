using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using backend;
using backend.Tests.Factories;
using backend.Models;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace backend.Tests
{
  public class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
  {
    private readonly HttpClient _client;

    public AccountControllerTests()
    {
      var factory = new TestFactory<Program>();
      _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WhenModelIsValid_ReturnsOk()
    {
      var model = new RegisterViewModel
      {
        UserName = "user123456",
        Email = "test@example.com",
        Password = "Password123!@#$%",
        ConfirmPassword = "Password123!@#$%"
      };

      var response = await _client.PostAsJsonAsync("/Account/Register", model);

      response.EnsureSuccessStatusCode();

      var json = await response.Content.ReadFromJsonAsync<JsonElement>();
      string? message = json.GetProperty("message").GetString();
      Assert.Equal("Registration successful and profile created.", message);
    }

    [Fact]
    public async Task Register_WhenModelIsInvalid_ReturnsBadRequest()
    {
      var model = new RegisterViewModel
      {
        UserName = "",
        Email = "not-an-email",
        Password = "short"
      };

      var response = await _client.PostAsJsonAsync("/Account/Register", model);

      Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
  }
}
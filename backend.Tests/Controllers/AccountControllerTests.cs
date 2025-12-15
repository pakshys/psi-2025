using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using backend;
using backend.Tests.Factories;
using backend.Models;
using backend.Database;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;

namespace backend.Tests
{
  public class AccountControllerTests : IClassFixture<WebApplicationFactory<Program>>
  {
    private readonly HttpClient _client;
    private readonly TestFactory<Program> _factory;

    public AccountControllerTests()
    {
      _factory = new TestFactory<Program>();
      _client = _factory.CreateClient();
    }

    private async Task<User> CreateTestUser(string username, string email, string password)
    {
      using var scope = _factory.Services.CreateScope();
      var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

      var user = new User
      {
        UserName = username,
        Email = email
      };

      await userManager.CreateAsync(user, password);
      return user;
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

    [Fact]
    public async Task Register_WhenUserAlreadyExists_ReturnsBadRequest()
    {
        var username = "existinguser";
        var email = "existing@example.com";
        var password = "Password123!";

        await CreateTestUser(username, email, password);

        var model = new RegisterViewModel
        {
            UserName = username,
            Email = "different@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var response = await _client.PostAsJsonAsync("/Account/Register", model);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? message = json.GetProperty("message").GetString();
        Assert.Equal("Registration failed", message);

        Assert.True(json.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Register_WhenPasswordsDontMatch_ReturnsBadRequest()
    {
        var model = new RegisterViewModel
        {
            UserName = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/Account/Register", model);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task Login_WithValidUsernameCredentials_ReturnsOk()
    {
      var username = "testuser";
      var email = "testuser@example.com";
      var password = "Password123!";

      await CreateTestUser(username, email, password);

      var loginModel = new LoginViewModel
      {
        Login = username,
        Password = password,
        RememberMe = false
      };

      var response = await _client.PostAsJsonAsync("/Account/Login", loginModel);

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);

      var json = await response.Content.ReadFromJsonAsync<JsonElement>();
      string? message = json.GetProperty("message").GetString();
      Assert.Equal("Login successful", message);
    }

    [Fact]
    public async Task Login_WithValidEmailCredentials_ReturnsOk()
    {
      var username = "emailuser";
      var email = "emailuser@example.com";
      var password = "Password123!";

      await CreateTestUser(username, email, password);

      var loginModel = new LoginViewModel
      {
        Login = email, // using email instead of username
        Password = password,
        RememberMe = true
      };

      var response = await _client.PostAsJsonAsync("/Account/Login", loginModel);

      Assert.Equal(HttpStatusCode.OK, response.StatusCode);

      var json = await response.Content.ReadFromJsonAsync<JsonElement>();
      string? message = json.GetProperty("message").GetString();
      Assert.Equal("Login successful", message);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
      var username = "validuser";
      var email = "validuser@example.com";
      var password = "Password123!";

      await CreateTestUser(username, email, password);

      var loginModel = new LoginViewModel
      {
        Login = username,
        Password = "WrongPassword123!",
        RememberMe = false
      };

      var response = await _client.PostAsJsonAsync("/Account/Login", loginModel);

      Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

      var json = await response.Content.ReadFromJsonAsync<JsonElement>();
      string? message = json.GetProperty("message").GetString();
      Assert.Equal("Invalid login attempt", message);
    }

    [Fact]
    public async Task Login_WithInvalidModelState_ReturnsBadRequest()
    {
      var loginModel = new LoginViewModel
      {
        Login = "", // empty login
        Password = "", // empty password
        RememberMe = false
      };

      var response = await _client.PostAsJsonAsync("/Account/Login", loginModel);

      Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenCalled_ReturnsOk()
    {
        var response = await _client.PostAsync("/Account/Logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? message = json.GetProperty("message").GetString();
        Assert.Equal("Logged out successfully", message);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/Account/Me");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        string? message = json.GetProperty("message").GetString();
        Assert.Equal("User not logged in", message);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserInfo()
    {
        var username = "authenticateduser";
        var email = "authenticated@example.com";
        var password = "Password123!";

        var user = await CreateTestUser(username, email, password);

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                username
            );

        var response = await _client.GetAsync("/Account/Me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        
        string? returnedId = json.GetProperty("id").GetString();
        string? returnedUsername = json.GetProperty("userName").GetString();
        string? returnedEmail = json.GetProperty("email").GetString();

        Assert.Equal(user.Id, returnedId);
        Assert.Equal(username, returnedUsername);
        Assert.Equal(email, returnedEmail);
    }
  }
}
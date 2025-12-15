using System.Net;
using System.Net.Http.Json;
using backend;
using backend.Database;
using backend.Models;
using backend.Services;
using backend.Controllers;
using backend.Tests.Factories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class UserProfileControllerTests
{
    private HttpClient _client = null!;

    private async Task<TestFactory<Program>> InitializeTestClient()
    {
        var factory = new TestFactory<Program>();
        _client = factory.CreateClient();

        await SeedDatabase(factory);

        return factory;
    }

    private async Task SeedDatabase(TestFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var alice = new User { UserName = "alice@test.com", Email = "alice@test.com" };
        var bob = new User { UserName = "bob@test.com", Email = "bob@test.com" };

        await userManager.CreateAsync(alice, "Password123!");
        await userManager.CreateAsync(bob, "Password123!");

        await db.SaveChangesAsync();
    }

    private void AuthenticateAs(string username)
    {
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                FakeAuthHandler.SchemeName,
                username
            );
    }

    [Fact]
    public async Task GetUserProfileById_ProfileMissing_ReturnsNotFound()
    {
        await InitializeTestClient();

        var response = await _client.GetAsync("/UserProfile/unknown-user-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }


    [Fact]
    public async Task GetUserProfileById_ProfileExists_ReturnsOk()
    {
        var factory = await InitializeTestClient();
        string aliceId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var alice = db.Users.First(u => u.UserName == "alice@test.com");
            aliceId = alice.Id;

            db.UserProfiles.Add(new UserProfile
            {
                UserId = alice.Id,
                DisplayName = "Alice",
                Bio = "Hello",
                ProfilePictureUrl = null
            });

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync($"/UserProfile/{aliceId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task GetCurrentUser_WhenUserIsNull_ReturnsUnauthorized()
    {
        await InitializeTestClient();

        var response = await _client.GetAsync("/UserProfile/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUserProfile()
    {
        var factory = await InitializeTestClient();

        AuthenticateAs("alice@test.com");

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var alice = db.Users.First(u => u.UserName == "alice@test.com");

            db.UserProfiles.Add(new UserProfile
            {
                UserId = alice.Id,
                DisplayName = "Alice",
                Bio = "Hello from Alice"
            });

            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/UserProfile/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task UpdateProfile_WhenUserIsNull_ReturnsBadRequest()
    {
        await InitializeTestClient();

        var response = await _client.PostAsJsonAsync("/UserProfile/update",
            new UserProfile
            {
                DisplayName = "New Name",
                Bio = "New Bio",
                ProfilePictureUrl = null
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenUserIsNull_ReturnsBadRequest()
    {
        await InitializeTestClient();

        var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/UserProfile/upload-picture", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_WhenFileMissingOrEmpty_ReturnsBadRequest()
    {
        await InitializeTestClient();
        AuthenticateAs("alice@test.com");

        // no file attached
        var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/UserProfile/upload-picture", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_OnSuccess_UpdatesProfileAndReturnsOk()
    {
        var factory = await InitializeTestClient();
        AuthenticateAs("alice@test.com");

        // create fake image
        var bytes = new byte[] { 1, 2, 3, 4 };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

        var form = new MultipartFormDataContent();
        form.Add(fileContent, "profile", "avatar.jpg");

        var response = await _client.PostAsync("/UserProfile/upload-picture", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify DB update
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var alice = db.Users.First(u => u.UserName == "alice@test.com");

        var profile = db.UserProfiles.FirstOrDefault(p => p.UserId == alice.Id);

        Assert.NotNull(profile);
        Assert.False(string.IsNullOrEmpty(profile!.ProfilePictureUrl));
        Assert.EndsWith("avatar.jpg", profile.ProfilePictureUrl);
    }

    [Fact]
    public async Task GetProfilePicture_WhenFileDoesNotExist_ReturnsNotFound()
    {
        await InitializeTestClient();

        var response = await _client.GetAsync("/UserProfile/picture/does-not-exist.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProfilePicture_WhenFileExists_ReturnsFile()
    {
        await InitializeTestClient();

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = "test-image.jpg";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // create fake image file
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4 });

        var response = await _client.GetAsync($"/UserProfile/picture/{fileName}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType!.MediaType);
    }
}

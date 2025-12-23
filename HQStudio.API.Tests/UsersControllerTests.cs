using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

public class UsersControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAdminAuth_ReturnsUsers()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/users");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDetailDto>>();
        users.Should().NotBeNull();
        users.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task GetById_ExistingUser_ReturnsUser()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/users/1");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        user.Should().NotBeNull();
        user!.Login.Should().Be("admin");
    }

    [Fact]
    public async Task GetById_NonExistingUser_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/users/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ExistingUser_ReturnsNoContent()
    {
        await AuthenticateAsync();

        var updateRequest = new { Name = "Updated Admin", Role = "Admin", Password = (string?)null };
        var response = await Client.PutAsJsonAsync("/api/users/1", updateRequest);
        
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_AdminUser_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        var response = await Client.DeleteAsync("/api/users/1");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_NonExistingUser_ReturnsNotFound()
    {
        await AuthenticateAsync();

        var response = await Client.DeleteAsync("/api/users/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private class UserDetailDto
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }
}

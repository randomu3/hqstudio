using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace HQStudio.API.Tests;

public class ActivityLogControllerTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/activitylog");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsActivityLogs()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/activitylog");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsCreatedLog()
    {
        await AuthenticateAsync();
        var request = new
        {
            action = "Тестовое действие",
            entityType = "Client",
            entityId = 123,
            details = "Дополнительная информация",
            source = "Web"
        };

        var response = await Client.PostAsJsonAsync("/api/activitylog", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<CreateActivityLogResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var request = new { action = "Test action" };

        var response = await Client.PostAsJsonAsync("/api/activitylog", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsStats()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/activitylog/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ActivityLogStats>();
        result.Should().NotBeNull();
        result!.TotalAll.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/activitylog/stats");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class ActivityLogResponse
    {
        public List<ActivityLogItem> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private class ActivityLogItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Action { get; set; } = "";
        public string Source { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    private class CreateActivityLogResponse
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class ActivityLogStats
    {
        public int TotalToday { get; set; }
        public int TotalWeek { get; set; }
        public int TotalAll { get; set; }
        public List<object> BySource { get; set; } = new();
        public List<object> ByUser { get; set; } = new();
    }
}

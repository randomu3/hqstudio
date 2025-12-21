using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.Controllers;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class CallbacksControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public CallbacksControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCallbackRequest("Иван Петров", "+79991234567", "BMW X5", null, "Хочу шумоизоляцию", null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MessageResponse>();
        result.Should().NotBeNull();
        result!.Message.Should().Contain("принята");
    }

    [Fact]
    public async Task Create_WithMinimalData_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateCallbackRequest("Мария", "+79998887766", null, null, null, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithSource_SetsSourceCorrectly()
    {
        // Arrange
        var request = new CreateCallbackRequest("Тест", "+79990001111", null, null, null, RequestSource.Phone, "Звонок с рекламы");

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithEmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCallbackRequest("", "+79991234567", null, null, null, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEmptyPhone_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateCallbackRequest("Тест", "", null, null, null, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithTooLongMessage_ReturnsBadRequest()
    {
        // Arrange
        var longMessage = new string('a', 1001);
        var request = new CreateCallbackRequest("Тест", "+79991234567", null, null, longMessage, null, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_WithAuth_ReturnsCallbacks()
    {
        // Arrange
        await AuthenticateAsync();

        // Create a callback first
        await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Test", "+79990001122", null, null, null, null, null));

        // Act
        var response = await _client.GetAsync("/api/callbacks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callbacks = await response.Content.ReadFromJsonAsync<List<CallbackRequest>>();
        callbacks.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_WithStatusFilter_ReturnsFilteredCallbacks()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/callbacks?status=New");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callbacks = await response.Content.ReadFromJsonAsync<List<CallbackRequest>>();
        callbacks.Should().NotBeNull();
        callbacks!.All(c => c.Status == RequestStatus.New).Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_WithSourceFilter_ReturnsFilteredCallbacks()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create callback with specific source
        await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Test Source", "+79990001133", null, null, null, null, null));

        // Act
        var response = await _client.GetAsync("/api/callbacks?source=Website");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callbacks = await response.Content.ReadFromJsonAsync<List<CallbackRequest>>();
        callbacks.Should().NotBeNull();
        callbacks!.All(c => c.Source == RequestSource.Website).Should().BeTrue();
    }

    [Fact]
    public async Task GetStats_WithAuth_ReturnsStats()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/callbacks/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<CallbackStats>();
        stats.Should().NotBeNull();
        stats!.TotalNew.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/callbacks/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_WithAuth_UpdatesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create a callback
        var createResponse = await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Status Test", "+79990001144", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        
        // Act
        var response = await _client.PutAsJsonAsync($"/api/callbacks/{createResult!.Id}/status", RequestStatus.Processing);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify
        var getResponse = await _client.GetAsync($"/api/callbacks/{createResult.Id}");
        var callback = await getResponse.Content.ReadFromJsonAsync<CallbackRequest>();
        callback!.Status.Should().Be(RequestStatus.Processing);
        callback.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create and process a callback
        var createResponse = await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Complete Test", "+79990001155", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        await _client.PutAsJsonAsync($"/api/callbacks/{createResult!.Id}/status", RequestStatus.Processing);
        
        // Act
        var response = await _client.PutAsJsonAsync($"/api/callbacks/{createResult.Id}/status", RequestStatus.Completed);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify
        var getResponse = await _client.GetAsync($"/api/callbacks/{createResult.Id}");
        var callback = await getResponse.Content.ReadFromJsonAsync<CallbackRequest>();
        callback!.Status.Should().Be(RequestStatus.Completed);
        callback.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_WithAdminAuth_DeletesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create a callback
        var createResponse = await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Delete Test", "+79990001166", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        
        // Act
        var response = await _client.DeleteAsync($"/api/callbacks/{createResult!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Verify deleted
        var getResponse = await _client.GetAsync($"/api/callbacks/{createResult.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NonExistent_ReturnsNotFound()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Act
        var response = await _client.DeleteAsync("/api/callbacks/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateManual_WithAuth_CreatesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new CreateCallbackRequest("Manual Test", "+79990001177", "Audi A4", "А123БВ77", "Тестовая заявка", RequestSource.WalkIn, "Пришёл в офис");

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks/manual", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callback = await response.Content.ReadFromJsonAsync<CallbackRequest>();
        callback.Should().NotBeNull();
        callback!.Name.Should().Be("Manual Test");
        callback.Source.Should().Be(RequestSource.WalkIn);
    }

    [Fact]
    public async Task CreateManual_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreateCallbackRequest("Manual Test", "+79990001188", null, null, null, RequestSource.WalkIn, null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/callbacks/manual", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithAuth_UpdatesCallback()
    {
        // Arrange
        await AuthenticateAsync();
        
        // Create a callback
        var createResponse = await _client.PostAsJsonAsync("/api/callbacks", new CreateCallbackRequest("Update Test", "+79990001199", null, null, null, null, null));
        var createResult = await createResponse.Content.ReadFromJsonAsync<MessageWithIdResponse>();
        
        var updateRequest = new UpdateCallbackRequest("Updated Name", "+79990002200", "Mercedes", "В456ГД99", "Обновлённое сообщение", null, null, null);

        // Act
        var response = await _client.PutAsJsonAsync($"/api/callbacks/{createResult!.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var callback = await response.Content.ReadFromJsonAsync<CallbackRequest>();
        callback!.Name.Should().Be("Updated Name");
        callback.CarModel.Should().Be("Mercedes");
        callback.LicensePlate.Should().Be("В456ГД99");
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }

    private record MessageResponse(string Message);
    private record MessageWithIdResponse(string Message, int Id);
}

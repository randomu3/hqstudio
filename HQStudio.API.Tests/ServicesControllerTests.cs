using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using HQStudio.API.DTOs;
using HQStudio.API.Models;
using Xunit;

namespace HQStudio.API.Tests;

public class ServicesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ServicesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_ReturnsServices()
    {
        // Act
        var response = await _client.GetAsync("/api/services");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var services = await response.Content.ReadFromJsonAsync<List<Service>>();
        services.Should().NotBeNull();
        services!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAll_WithActiveOnly_ReturnsOnlyActiveServices()
    {
        // Act
        var response = await _client.GetAsync("/api/services?activeOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var services = await response.Content.ReadFromJsonAsync<List<Service>>();
        services.Should().NotBeNull();
        services!.Should().OnlyContain(s => s.IsActive);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsService()
    {
        // Act
        var response = await _client.GetAsync("/api/services/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var service = await response.Content.ReadFromJsonAsync<Service>();
        service.Should().NotBeNull();
        service!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/services/9999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var service = new Service { Title = "Test", Category = "Test", Description = "Test", Price = "100" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", service);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithAuth_CreatesService()
    {
        // Arrange
        await AuthenticateAsync();
        var service = new { Title = "New Service", Category = "Test", Description = "Test Description", Price = "от 5000 ₽" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/services", service);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<Service>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("New Service");
    }

    [Fact]
    public async Task Update_WithDesktopClient_UpdatesServiceIcon()
    {
        // Arrange - добавляем заголовок Desktop клиента
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        
        // Сначала получаем существующую услугу
        var getResponse = await _client.GetAsync("/api/services/1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var existingService = await getResponse.Content.ReadFromJsonAsync<Service>();
        existingService.Should().NotBeNull();
        
        // Меняем иконку (PascalCase как ожидает API)
        var updatedService = new 
        { 
            Id = existingService!.Id,
            Title = existingService.Title,
            Category = existingService.Category,
            Description = existingService.Description,
            Price = existingService.Price,
            Image = existingService.Image,
            Icon = "🎨",  // Новая иконка
            IsActive = existingService.IsActive,
            SortOrder = existingService.SortOrder
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/services/{existingService.Id}", updatedService);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Проверяем что иконка обновилась
        var verifyResponse = await _client.GetAsync($"/api/services/{existingService.Id}");
        var verifiedService = await verifyResponse.Content.ReadFromJsonAsync<Service>();
        verifiedService!.Icon.Should().Be("🎨");
    }

    [Fact]
    public async Task Update_WithMismatchedId_ReturnsBadRequest()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        var service = new { Id = 999, Title = "Test", Category = "Test", Description = "Test", Price = "100", Icon = "🔧", IsActive = true, SortOrder = 0 };

        // Act
        var response = await _client.PutAsJsonAsync("/api/services/1", service);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WithDesktopClient_UpdatesAllFields()
    {
        // Arrange - эмулируем точно то, что делает Desktop клиент
        _client.DefaultRequestHeaders.Add("X-Client-Type", "Desktop");
        
        // Сначала получаем существующую услугу
        var getResponse = await _client.GetAsync("/api/services/1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var existingService = await getResponse.Content.ReadFromJsonAsync<Service>();
        existingService.Should().NotBeNull();
        
        // Создаём объект как в Desktop клиенте (PascalCase)
        var updatedService = new 
        { 
            Id = existingService!.Id,
            Title = "Обновлённая услуга",
            Category = "Новая категория",
            Description = "Новое описание",
            Price = "от 20000 ₽",
            Image = (string?)null,
            Icon = "🚗",
            IsActive = true,
            SortOrder = existingService.SortOrder
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/services/{existingService.Id}", updatedService);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        
        // Проверяем что все поля обновились
        var verifyResponse = await _client.GetAsync($"/api/services/{existingService.Id}");
        var verifiedService = await verifyResponse.Content.ReadFromJsonAsync<Service>();
        verifiedService!.Title.Should().Be("Обновлённая услуга");
        verifiedService.Category.Should().Be("Новая категория");
        verifiedService.Description.Should().Be("Новое описание");
        verifiedService.Icon.Should().Be("🚗");
    }

    private async Task AuthenticateAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin", "admin"));
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Token);
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HQStudio.API.Data;
using HQStudio.API.Models;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Testcontainers.PostgreSql;
using Xunit;

namespace HQStudio.API.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("hqstudio_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add PostgreSQL from Testcontainers
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseNpgsql(_postgres.GetConnectionString());
            });
        });

        builder.UseEnvironment("Testing");
    }

    public void SeedDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Создаём таблицы
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        // Add test user
        db.Users.Add(new User
        {
            Login = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            Name = "Администратор",
            Role = UserRole.Admin
        });

        // Add test services
        db.Services.AddRange(
            new Service { Title = "Доводчики дверей", Category = "Комфорт", Description = "Test", Price = "от 15 000 ₽", Icon = "🚪", IsActive = true, SortOrder = 1 },
            new Service { Title = "Шумоизоляция", Category = "Тишина", Description = "Test", Price = "от 15 000 ₽", Icon = "🔇", IsActive = true, SortOrder = 2 }
        );

        // Add test blocks
        db.SiteBlocks.AddRange(
            new SiteBlock { BlockId = "hero", Name = "Главный экран", Enabled = true, SortOrder = 1 },
            new SiteBlock { BlockId = "services", Name = "Услуги", Enabled = true, SortOrder = 2 }
        );

        // Add test testimonials
        db.Testimonials.Add(new Testimonial { Name = "Марина", Car = "Audi Q7", Text = "Отличный сервис!", IsActive = true, SortOrder = 1 });

        // Add test FAQ
        db.FaqItems.Add(new FaqItem { Question = "Сохранится ли дилерская гарантия?", Answer = "Да", IsActive = true, SortOrder = 1 });

        db.SaveChanges();
    }

    /// <summary>
    /// Creates an authenticated HTTP client with admin credentials
    /// </summary>
    public async Task<HttpClient> GetAuthenticatedClientAsync(string login = "admin", string password = "admin")
    {
        SeedDatabase();
        var client = CreateClient();
        
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new { login, password });
        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Failed to authenticate: {loginResponse.StatusCode}");
        }
        
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
        
        return client;
    }

    private class LoginResult
    {
        public string Token { get; set; } = "";
    }
}

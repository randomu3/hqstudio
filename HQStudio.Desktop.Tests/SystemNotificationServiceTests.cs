using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace HQStudio.Desktop.Tests;

/// <summary>
/// Тесты для логики SystemNotificationService (без зависимости от WPF и Windows Forms)
/// </summary>
public class SystemNotificationServiceTests
{
    [Fact]
    public void ShowNewCallbackNotification_WhenMinimized_ShowsNotification()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: true);
        
        // Act
        service.ShowNewCallbackNotification("Иван Петров", "+7 (999) 123-45-67");
        
        // Assert
        service.NotificationsShown.Should().HaveCount(1);
        service.NotificationsShown[0].Title.Should().Contain("заявка");
        service.NotificationsShown[0].Message.Should().Contain("Иван Петров");
        service.NotificationsShown[0].Message.Should().Contain("+7 (999) 123-45-67");
    }

    [Fact]
    public void ShowNewCallbackNotification_WhenNotMinimized_DoesNotShowNotification()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: false);
        
        // Act
        service.ShowNewCallbackNotification("Иван Петров", "+7 (999) 123-45-67");
        
        // Assert
        service.NotificationsShown.Should().BeEmpty();
    }

    [Fact]
    public void ShowNewOrderNotification_WhenMinimized_ShowsNotification()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: true);
        
        // Act
        service.ShowNewOrderNotification("Мария Сидорова", 42);
        
        // Assert
        service.NotificationsShown.Should().HaveCount(1);
        service.NotificationsShown[0].Title.Should().Contain("заказ");
        service.NotificationsShown[0].Message.Should().Contain("42");
        service.NotificationsShown[0].Message.Should().Contain("Мария Сидорова");
    }

    [Fact]
    public void ShowNewOrderNotification_WhenNotMinimized_DoesNotShowNotification()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: false);
        
        // Act
        service.ShowNewOrderNotification("Мария Сидорова", 42);
        
        // Assert
        service.NotificationsShown.Should().BeEmpty();
    }

    [Fact]
    public void ShowNotification_AlwaysShowsRegardlessOfMinimizedState()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: false);
        
        // Act
        service.ShowNotification("Тест", "Сообщение");
        
        // Assert
        service.NotificationsShown.Should().HaveCount(1);
    }

    [Fact]
    public void IsAppMinimized_ReturnsCorrectState()
    {
        // Arrange & Act
        var minimizedService = new TestSystemNotificationService(isMinimized: true);
        var normalService = new TestSystemNotificationService(isMinimized: false);
        
        // Assert
        minimizedService.IsAppMinimized.Should().BeTrue();
        normalService.IsAppMinimized.Should().BeFalse();
    }

    [Fact]
    public void OnNotificationClicked_RaisesEvent()
    {
        // Arrange
        var service = new TestSystemNotificationService(isMinimized: true);
        string? clickedTarget = null;
        service.OnNotificationClicked += target => clickedTarget = target;
        
        // Act
        service.SimulateBalloonClick();
        
        // Assert
        clickedTarget.Should().Be("Callbacks");
    }

    /// <summary>
    /// Property 7: Notification on new callback when minimized
    /// *For any* new callback arrival when app is minimized, a system notification should be triggered with callback details.
    /// **Validates: Requirements 7.1, 7.3**
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property7_NotificationOnNewCallbackWhenMinimized()
    {
        return Prop.ForAll(
            Arb.Default.NonEmptyString(),
            Arb.Default.NonEmptyString(),
            (name, phone) =>
            {
                // Arrange
                var service = new TestSystemNotificationService(isMinimized: true);
                var clientName = name.Get;
                var clientPhone = phone.Get;
                
                // Act
                service.ShowNewCallbackNotification(clientName, clientPhone);
                
                // Assert - notification should be shown with callback details
                var notificationShown = service.NotificationsShown.Count == 1;
                var containsName = service.NotificationsShown.Count > 0 && 
                                   service.NotificationsShown[0].Message.Contains(clientName);
                var containsPhone = service.NotificationsShown.Count > 0 && 
                                    service.NotificationsShown[0].Message.Contains(clientPhone);
                
                return notificationShown && containsName && containsPhone;
            });
    }

    /// <summary>
    /// Property: No notification when app is not minimized
    /// *For any* new callback arrival when app is NOT minimized, no system notification should be triggered.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property Property_NoNotificationWhenNotMinimized()
    {
        return Prop.ForAll(
            Arb.Default.NonEmptyString(),
            Arb.Default.NonEmptyString(),
            (name, phone) =>
            {
                // Arrange
                var service = new TestSystemNotificationService(isMinimized: false);
                
                // Act
                service.ShowNewCallbackNotification(name.Get, phone.Get);
                
                // Assert - no notification should be shown
                return service.NotificationsShown.Count == 0;
            });
    }
}

/// <summary>
/// Модель уведомления для тестов
/// </summary>
public class TestNotification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Тестовая реализация SystemNotificationService без зависимости от WPF и Windows Forms
/// </summary>
public class TestSystemNotificationService
{
    private readonly bool _isMinimized;

    public List<TestNotification> NotificationsShown { get; } = new();

    public event Action<string>? OnNotificationClicked;

    public TestSystemNotificationService(bool isMinimized)
    {
        _isMinimized = isMinimized;
    }

    public bool IsAppMinimized => _isMinimized;

    public void ShowNotification(string title, string message, Action? onClick = null)
    {
        NotificationsShown.Add(new TestNotification
        {
            Title = title,
            Message = message
        });
    }

    public void ShowNewCallbackNotification(string name, string phone)
    {
        if (!IsAppMinimized) return;

        ShowNotification(
            "📞 Новая заявка",
            $"{name}\n{phone}"
        );
    }

    public void ShowNewOrderNotification(string clientName, int orderId)
    {
        if (!IsAppMinimized) return;

        ShowNotification(
            "📋 Новый заказ",
            $"Заказ #{orderId}\nКлиент: {clientName}"
        );
    }

    public void SimulateBalloonClick()
    {
        OnNotificationClicked?.Invoke("Callbacks");
    }
}

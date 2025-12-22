namespace HQStudio.API.Models;

public enum UserRole
{
    Admin,
    Editor,
    Manager
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Manager;
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Раздельные права доступа
    public bool CanAccessWeb { get; set; } = true;
    public bool CanAccessDesktop { get; set; } = true;
    
    // Роли для разных платформ (если нужны разные права)
    public UserRole? WebRole { get; set; }
    public UserRole? DesktopRole { get; set; }
    
    /// <summary>
    /// Получить роль для конкретной платформы
    /// </summary>
    public UserRole GetRoleForPlatform(string platform)
    {
        return platform.ToLower() switch
        {
            "web" => WebRole ?? Role,
            "desktop" => DesktopRole ?? Role,
            _ => Role
        };
    }
}

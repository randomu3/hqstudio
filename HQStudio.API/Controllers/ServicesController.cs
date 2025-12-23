using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(AppDbContext db, ILogger<ServicesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private bool IsDesktopClient()
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        return clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
    }

    [HttpGet]
    public async Task<ActionResult<List<Service>>> GetAll([FromQuery] bool activeOnly = false)
    {
        _logger.LogInformation("[Services] GET all, activeOnly={ActiveOnly}, Desktop={IsDesktop}", activeOnly, IsDesktopClient());
        var query = _db.Services.AsQueryable();
        if (activeOnly) query = query.Where(s => s.IsActive);
        var result = await query.OrderBy(s => s.SortOrder).ToListAsync();
        _logger.LogInformation("[Services] Returning {Count} services", result.Count);
        return result;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> Get(int id)
    {
        _logger.LogInformation("[Services] GET {Id}", id);
        var service = await _db.Services.FindAsync(id);
        if (service == null)
        {
            _logger.LogWarning("[Services] Service {Id} not found", id);
            return NotFound();
        }
        _logger.LogInformation("[Services] Found service: Id={Id}, Title={Title}, Icon={Icon}", service.Id, service.Title, service.Icon);
        return Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<Service>> Create(Service service)
    {
        _logger.LogInformation("[Services] POST Create: Title={Title}, Icon={Icon}, Desktop={IsDesktop}", 
            service.Title, service.Icon, IsDesktopClient());
        
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("[Services] Unauthorized create attempt");
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        if (!IsDesktopClient() && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
        {
            _logger.LogWarning("[Services] Forbidden create attempt");
            return Forbid();
        }

        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[Services] Created service Id={Id}", service.Id);
        return CreatedAtAction(nameof(Get), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Service service)
    {
        _logger.LogInformation("[Services] PUT Update {Id}: Title={Title}, Icon={Icon}, Desktop={IsDesktop}", 
            id, service.Title, service.Icon, IsDesktopClient());
        
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            _logger.LogWarning("[Services] Unauthorized update attempt for {Id}", id);
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        if (!IsDesktopClient() && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
        {
            _logger.LogWarning("[Services] Forbidden update attempt for {Id}", id);
            return Forbid();
        }

        if (id != service.Id)
        {
            _logger.LogWarning("[Services] ID mismatch: URL={UrlId}, Body={BodyId}", id, service.Id);
            return BadRequest(new { message = $"ID mismatch: URL={id}, Body={service.Id}" });
        }
        
        var existing = await _db.Services.FindAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("[Services] Service {Id} not found for update", id);
            return NotFound();
        }
        
        _logger.LogInformation("[Services] Updating service {Id}: OldIcon={OldIcon} -> NewIcon={NewIcon}", 
            id, existing.Icon, service.Icon);
        
        existing.Title = service.Title;
        existing.Category = service.Category;
        existing.Description = service.Description;
        existing.Price = service.Price;
        existing.Image = service.Image;
        existing.Icon = service.Icon;
        existing.IsActive = service.IsActive;
        existing.SortOrder = service.SortOrder;
        
        await _db.SaveChangesAsync();
        _logger.LogInformation("[Services] Successfully updated service {Id}", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("[Services] DELETE {Id}, Desktop={IsDesktop}", id, IsDesktopClient());
        
        // Для веб-клиентов требуется авторизация Admin
        if (!IsDesktopClient())
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("[Services] Unauthorized delete attempt for {Id}", id);
                return Unauthorized(new { message = "Требуется авторизация" });
            }
            if (!User.IsInRole("Admin"))
            {
                _logger.LogWarning("[Services] Forbidden delete attempt for {Id}", id);
                return Forbid();
            }
        }

        var service = await _db.Services.FindAsync(id);
        if (service == null)
        {
            _logger.LogWarning("[Services] Service {Id} not found for delete", id);
            return NotFound();
        }
        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        _logger.LogInformation("[Services] Successfully deleted service {Id}", id);
        return NoContent();
    }
}

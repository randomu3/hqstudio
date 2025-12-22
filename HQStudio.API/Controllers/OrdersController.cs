using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;
using System.Security.Claims;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    private bool IsDesktopClient()
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        return clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetAll(
        [FromQuery] OrderStatus? status = null, 
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeDeleted = false)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }

        var query = _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .AsQueryable();

        // По умолчанию не показываем удалённые заказы
        if (!includeDeleted)
        {
            query = query.Where(o => !o.IsDeleted);
        }

        if (status.HasValue) query = query.Where(o => o.Status == status);

        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> Get(int id)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }

        var order = await _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create(CreateOrderRequest request)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }

        var order = new Order
        {
            ClientId = request.ClientId,
            Notes = request.Notes,
            TotalPrice = request.TotalPrice
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var serviceId in request.ServiceIds)
        {
            var service = await _db.Services.FindAsync(serviceId);
            if (service != null)
            {
                _db.OrderServices.Add(new OrderService
                {
                    OrderId = order.Id,
                    ServiceId = serviceId,
                    Price = 0 // Can be set from service price
                });
            }
        }
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        if (status == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Soft delete - помечает заказ как удалённый, но не удаляет из базы
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Для веб-клиентов требуется авторизация с ролью Admin
        if (!IsDesktopClient())
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { message = "Требуется авторизация" });
            }
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        var userId = User.Identity?.IsAuthenticated == true 
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
            : 0;
        
        // Soft delete - помечаем как удалённый
        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        order.DeletedByUserId = userId;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Восстановление удалённого заказа
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        // Для веб-клиентов требуется авторизация с ролью Admin
        if (!IsDesktopClient())
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { message = "Требуется авторизация" });
            }
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }

        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        
        order.IsDeleted = false;
        order.DeletedAt = null;
        order.DeletedByUserId = null;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Полное удаление заказа (только для разработчиков)
    /// </summary>
    [HttpDelete("{id}/permanent")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PermanentDelete(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderServices)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        if (order == null) return NotFound();
        
        // Удаляем связанные услуги
        _db.OrderServices.RemoveRange(order.OrderServices);
        _db.Orders.Remove(order);
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Удаление заказов без клиентов (очистка базы)
    /// </summary>
    [HttpDelete("cleanup/without-clients")]
    public async Task<ActionResult<object>> CleanupOrdersWithoutClients()
    {
        // Только для Desktop клиента
        if (!IsDesktopClient())
        {
            return Forbid();
        }

        var clientIds = await _db.Clients.Select(c => c.Id).ToListAsync();
        
        var ordersWithoutClients = await _db.Orders
            .Include(o => o.OrderServices)
            .Where(o => o.ClientId == 0 || !clientIds.Contains(o.ClientId))
            .ToListAsync();

        if (!ordersWithoutClients.Any())
        {
            return Ok(new { message = "Заказов без клиентов не найдено", deleted = 0 });
        }

        var deletedIds = ordersWithoutClients.Select(o => o.Id).ToList();
        
        foreach (var order in ordersWithoutClients)
        {
            _db.OrderServices.RemoveRange(order.OrderServices);
        }
        
        _db.Orders.RemoveRange(ordersWithoutClients);
        await _db.SaveChangesAsync();

        return Ok(new { 
            message = $"Удалено {ordersWithoutClients.Count} заказов без клиентов", 
            deleted = ordersWithoutClients.Count,
            deletedIds 
        });
    }
}

public record CreateOrderRequest(int ClientId, List<int> ServiceIds, decimal TotalPrice, string? Notes);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;
using HQStudio.API.Services;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db) => _db = db;

    private bool IsDesktopClient()
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        return clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
    }

    [HttpGet]
    public async Task<ActionResult<List<Client>>> GetAll([FromQuery] int? limit = null)
    {
        // Desktop клиент может работать без авторизации
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized();
        }
        
        var query = _db.Clients.OrderByDescending(c => c.CreatedAt).AsQueryable();
        
        if (!IsDesktopClient())
        {
            query = query.Take(20);
        }
        else if (limit.HasValue && limit > 0)
        {
            query = query.Take(limit.Value);
        }
        
        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Client>> Get(int id)
    {
        var client = await _db.Clients.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
        return client == null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create(Client client)
    {
        // Desktop клиент может создавать клиентов без авторизации
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized();
        }
        
        // Форматируем телефон
        client.Phone = PhoneFormatter.Format(client.Phone);
        
        // Проверка на дубликат по телефону
        var normalizedPhone = client.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        var existingByPhone = await _db.Clients
            .FirstOrDefaultAsync(c => c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "") == normalizedPhone);
        
        if (existingByPhone != null)
        {
            return Conflict(new { message = "Клиент с таким номером телефона уже существует", existingClientId = existingByPhone.Id, existingClientName = existingByPhone.Name });
        }
        
        client.CreatedAt = DateTime.UtcNow;
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = client.Id }, client);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, Client client)
    {
        if (id != client.Id) return BadRequest();
        
        var existing = await _db.Clients.FindAsync(id);
        if (existing == null) return NotFound();
        
        existing.Name = client.Name;
        existing.Phone = PhoneFormatter.Format(client.Phone);
        existing.Email = client.Email;
        existing.CarModel = client.CarModel;
        existing.LicensePlate = client.LicensePlate;
        existing.Notes = client.Notes;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

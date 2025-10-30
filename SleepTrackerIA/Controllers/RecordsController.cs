using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SleepTrackerIA.Data;
using SleepTrackerIA.Models;

namespace SleepTrackerIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecordsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RecordsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var query = _db.SleepRecords.OrderByDescending(r => r.Date);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { items, total });
    }

    public record CreateRecordDto(string SleepTime, string WakeTime, int ProductivityMorning, int ProductivityAfternoon, int ProductivityNight, DateTime? Date);

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateRecordDto dto)
    {
        var record = new SleepRecord
        {
            Date = dto.Date ?? DateTime.Today.AddDays(-1),
            SleepTime = dto.SleepTime,
            WakeTime = dto.WakeTime,
            ProductivityMorning = dto.ProductivityMorning,
            ProductivityAfternoon = dto.ProductivityAfternoon,
            ProductivityNight = dto.ProductivityNight
        };

        _db.SleepRecords.Add(record);
        await _db.SaveChangesAsync();
        return Created($"/api/records/{record.Id}", record);
    }
}
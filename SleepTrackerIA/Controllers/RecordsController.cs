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

        var query = _db.SleepRecords.OrderByDescending(r => r.StartTime);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return Ok(new { items, total });
    }

    public record CreateRecordDto(
        DateTime startTime, 
        DateTime endTime,  
        string? notes,      
        int productivityMorning, 
        int productivityAfternoon, 
        int productivityNight 
    );

    //[HttpPost]
    //public async Task<IActionResult> Post([FromBody] CreateRecordDto dto)
    //{
    //    var record = new SleepRecord
    //    {
    //        StartTime = dto.startTime, 
    //        EndTime = dto.endTime,    
    //        Notes = dto.notes,        
    //        ProductivityMorning = dto.productivityMorning, 
    //        ProductivityAfternoon = dto.productivityAfternoon, 
    //        ProductivityNight = dto.productivityNight 
    //    };

    //    _db.SleepRecords.Add(record);
    //    await _db.SaveChangesAsync();
    //    return Created($"/api/records/{record.Id}", record);
    //}

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var record = await _db.SleepRecords.FindAsync(id);
        if (record == null)
        {
            return NotFound();
        }

        _db.SleepRecords.Remove(record);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SleepTrackerIA.Data;

namespace SleepTrackerIA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var last = await _db.SleepRecords.OrderByDescending(r => r.Date).FirstOrDefaultAsync();
        if (last == null)
        {
            return Ok(new
            {
                forecast = new
                {
                    morning = new { level = "N/A", score = 0 },
                    afternoon = new { level = "N/A", score = 0 },
                    night = new { level = "N/A", score = 0 }
                },
                recommendation = DefaultRecommendation()
            });
        }

        string Level(int score) => score >= 4 ? "Alta" : score >= 3 ? "Média" : "Baixa";

        var result = new
        {
            forecast = new
            {
                morning = new { level = Level(last.ProductivityMorning), score = last.ProductivityMorning },
                afternoon = new { level = Level(last.ProductivityAfternoon), score = last.ProductivityAfternoon },
                night = new { level = Level(last.ProductivityNight), score = last.ProductivityNight }
            },
            recommendation = DefaultRecommendation()
        };

        return Ok(result);
    }

    private object DefaultRecommendation() => new
    {
        sleepAt = "22:45",
        wakeAt = "06:30",
        note = "Essas sugestões serão substituídas pela sua API Python"
    };
}
using Microsoft.EntityFrameworkCore;
using SleepTrackerIA.Data;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=sleeptracker.db"));

var app = builder.Build();

// Create DB on startup and seed a sample row if empty
using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    ctx.Database.EnsureCreated();
    if (!ctx.SleepRecords.Any())
    {
        ctx.SleepRecords.Add(new SleepTrackerIA.Models.SleepRecord
        {
            Date = DateTime.Today.AddDays(-1),
            SleepTime = "23:00",
            WakeTime = "07:00",
            ProductivityMorning = 3,
            ProductivityAfternoon = 3,
            ProductivityNight = 3
        });
        ctx.SaveChanges();
    }
}

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using SleepTrackerIA.Models;

namespace SleepTrackerIA.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<SleepRecord> SleepRecords => Set<SleepRecord>();
}
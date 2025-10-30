namespace SleepTrackerIA.Models;

public class SleepRecord
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string SleepTime { get; set; } = ""; // HH:mm
    public string WakeTime { get; set; } = "";  // HH:mm
    public int ProductivityMorning { get; set; } // 1-5
    public int ProductivityAfternoon { get; set; } // 1-5
    public int ProductivityNight { get; set; } // 1-5
}
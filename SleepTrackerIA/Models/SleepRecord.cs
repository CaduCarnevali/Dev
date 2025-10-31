namespace SleepTrackerIA.Models
{
    public class SleepRecord
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int ProductivityMorning { get; set; }
        public int ProductivityAfternoon { get; set; }
        public int ProductivityNight { get; set; }
        public string? Notes { get; set; }

        // O modelo de IA precisa da Duração em horas
        public double DurationInHours => (EndTime - StartTime).TotalHours;
    }
}
namespace BusinessLogic.DTOs
{
    /// <summary>
    /// Statistics về hoạt động của Event Scheduler
    /// </summary>
    public class SchedulerStatistics
    {
        public DateTime? LastRunTime { get; set; }
        public DateTime? NextRunTime { get; set; }
        public int TotalEventsEnded { get; set; }
        public int LastRunEventsEnded { get; set; }
        public int TotalRuns { get; set; }
        public int FailedRuns { get; set; }
        public bool IsRunning { get; set; }
        public TimeSpan CheckInterval { get; set; }
    }
}


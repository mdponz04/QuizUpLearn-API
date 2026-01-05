using BusinessLogic.DTOs;

namespace BusinessLogic.Interfaces
{
    /// <summary>
    /// Interface cho Event Scheduler Background Service
    /// Tự động cập nhật status và ranks cho Events đã hết hạn
    /// </summary>
    public interface IEventSchedulerService
    {
        /// <summary>
        /// Lấy thông tin về lần chạy cuối cùng của scheduler
        /// </summary>
        DateTime? LastRunTime { get; }

        /// <summary>
        /// Lấy thời gian chạy tiếp theo dự kiến
        /// </summary>
        DateTime? NextRunTime { get; }

        /// <summary>
        /// Số Events đã được end trong lần chạy cuối
        /// </summary>
        int LastRunEventsEnded { get; }

        /// <summary>
        /// Kiểm tra scheduler có đang chạy không
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Force trigger check ngay lập tức (không chờ interval)
        /// Useful cho testing hoặc manual trigger
        /// </summary>
        Task TriggerCheckNowAsync();

        /// <summary>
        /// Lấy statistics về hoạt động của scheduler
        /// </summary>
        Task<SchedulerStatistics> GetStatisticsAsync();
    }
}


using BusinessLogic.DTOs.AdminDashboardDtos;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizUpLearn.API.Attributes;

namespace QuizUpLearn.API.Controllers
{
    [Route("api/admin/dashboard")]
    [ApiController]
    [Authorize]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAdminDashboardService _service;

        public AdminDashboardController(IAdminDashboardService service)
        {
            _service = service;
        }

        [HttpGet("overview")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetOverview()
        {
            var result = await _service.GetOverviewAsync();
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("revenue")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetRevenue(
            [FromQuery] string timeRange = "month",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _service.GetRevenueAsync(timeRange, startDate, endDate);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("transaction-status")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetTransactionStatus()
        {
            var result = await _service.GetTransactionStatusDistributionAsync();
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("user-growth")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetUserGrowth([FromQuery] int months = 12)
        {
            var result = await _service.GetUserGrowthAsync(months);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("event-stats")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetEventStats(
            [FromQuery] int limit = 10,
            [FromQuery] string sortBy = "participants")
        {
            var result = await _service.GetEventStatsAsync(limit, sortBy);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("tournament-stats")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetTournamentStats(
            [FromQuery] int limit = 10,
            [FromQuery] string sortBy = "participants")
        {
            var result = await _service.GetTournamentStatsAsync(limit, sortBy);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        /*[HttpGet("ai-usage")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetAIUsage(
            [FromQuery] string timeRange = "week",
            [FromQuery] string groupBy = "week")
        {
            var result = await _service.GetAIUsageAsync(timeRange, groupBy);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }*/

        [HttpGet("subscription-distribution")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetSubscriptionDistribution()
        {
            var result = await _service.GetSubscriptionDistributionAsync();
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("quizset-stats")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetQuizSetStats()
        {
            var result = await _service.GetQuizSetStatsAsync();
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }

        [HttpGet("activity-summary")]
        [SubscriptionAndRoleAuthorize("Administrator")]
        public async Task<IActionResult> GetActivitySummary([FromQuery] int days = 7)
        {
            var result = await _service.GetActivitySummaryAsync(days);
            return Ok(new { Success = true, Data = result, Message = "Success" });
        }
    }
}


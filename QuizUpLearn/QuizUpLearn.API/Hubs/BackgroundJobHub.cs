using Microsoft.AspNetCore.SignalR;

namespace QuizUpLearn.API.Hubs
{
    public class BackgroundJobHub : Hub
    {
        private readonly ILogger<BackgroundJobHub> _logger;

        public BackgroundJobHub(ILogger<BackgroundJobHub> logger)
        {
            _logger = logger;
        }
        public override async Task OnConnectedAsync()
        {
            var role = Context.User?.FindFirst("roleName")?.Value;
            var userId = Context.User?.FindFirst("userId")?.Value;
            //group by role
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            }
            //personal group by userId
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            }

            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("Connected", $"Welcome! Your connection ID is {Context.ConnectionId}.");
            await base.OnConnectedAsync();
        }

        public async Task JoinJobGroup(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
            _logger.LogInformation($"Client {Context.ConnectionId} joined job group {jobId}");
        }
        //Test purpose
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
            _logger.LogInformation($"Client {Context.ConnectionId} joined user group: user:{userId}");
        }
        public async Task LeaveJobGroup(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
            _logger.LogInformation($"Client {Context.ConnectionId} leave job group {jobId}");
        }
    }
}

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
            _logger.LogInformation($"Client connected: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("Connected", $"Welcome! Your connection ID is {Context.ConnectionId}.");
            await base.OnConnectedAsync();
        }

        public async Task JoinJobGroup(string jobId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, jobId);
            _logger.LogInformation($"Client {Context.ConnectionId} joined job group {jobId}");
        }
        
        public async Task LeaveJobGroup(string jobId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, jobId);
            _logger.LogInformation($"Client {Context.ConnectionId} leave job group {jobId}");
        }

        /// <summary>
        /// Join a user-specific notification group to receive real-time notifications
        /// </summary>
        /// <param name="userId">The user's ID</param>
        public async Task JoinNotificationGroup(string userId)
        {
            var groupName = $"notifications_{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} joined notification group {groupName}");
        }

        /// <summary>
        /// Leave a user-specific notification group
        /// </summary>
        /// <param name="userId">The user's ID</param>
        public async Task LeaveNotificationGroup(string userId)
        {
            var groupName = $"notifications_{userId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation($"Client {Context.ConnectionId} left notification group {groupName}");
        }
    }
}

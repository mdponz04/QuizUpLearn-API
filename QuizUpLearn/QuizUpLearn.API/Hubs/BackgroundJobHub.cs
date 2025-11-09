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
    }
}

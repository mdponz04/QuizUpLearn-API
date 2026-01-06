using Microsoft.AspNetCore.SignalR.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting SignalR Client...");

        Console.WriteLine("Enter User ID to monitor notifications:");
        string userId = Console.ReadLine();
        Console.WriteLine($"Monitoring notifications for User ID: {userId}");

        var hubUrl = "https://localhost:7247/background-jobs";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        //Listen for user-specific notifications
        connection.On<object>("NotificationCreated", notification =>
        {
            Console.WriteLine("=== Notification Created ===");
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(notification, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine("=============================");
        });

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to SignalR hub!");

            //Join user-specific group instead of job group
            await connection.InvokeAsync("JoinUserGroup", userId);
            Console.WriteLine($"Joined user group: user:{userId}");

            Console.WriteLine("Listening for notifications... Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}

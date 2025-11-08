using Microsoft.AspNetCore.SignalR.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting SignalR Client...");

        // Replace with your actual SignalR hub URL
        var hubUrl = "https://localhost:7247/background-jobs";

        // Create a connection to the SignalR hub
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect() // Automatically reconnect on disconnection
            .Build();

        // Event listener for the "Connected" event
        connection.On<string>("Connected", message =>
        {
            Console.WriteLine($"Message from server: {message}");
        });

        // Event listener for the "JobCompleted" event
        connection.On<object>("JobCompleted", job =>
        {
            Console.WriteLine("Job Completed:");
            Console.WriteLine(job);
        });

        // Event listener for the "JobFailed" event
        connection.On<object>("JobFailed", job =>
        {
            Console.WriteLine("Job Failed:");
            Console.WriteLine(job);
        });

        try
        {
            // Start the connection
            await connection.StartAsync();
            Console.WriteLine("Connected to SignalR hub!");

            // Keep the client running
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }
}
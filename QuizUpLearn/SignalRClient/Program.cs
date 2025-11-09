using Microsoft.AspNetCore.SignalR.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting SignalR Client...");

        Console.WriteLine("Enter Job ID to monitor:");
        string jobId = Console.ReadLine();
        Console.WriteLine($"Monitoring Job ID: {jobId}");

        var hubUrl = "https://qul-api.onrender.com/background-jobs";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        connection.On<object>("JobCompleted", job =>
        {
            Console.WriteLine("Job Completed:");
            Console.WriteLine(job);
        });
        connection.On<object>("JobFailed", job =>
        {
            Console.WriteLine("Job Failed:");
            Console.WriteLine(job);
        });

        try
        {
            await connection.StartAsync();
            Console.WriteLine("Connected to SignalR hub!");

            await connection.InvokeAsync("JoinJobGroup", jobId);
            Console.WriteLine($"Joined job group: {jobId}");

            Console.WriteLine("Listening for job updates... Press any key to exit.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
        }
    }
}

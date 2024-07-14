using System;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    static async Task Main(string[] args)
    {
        // Number of concurrent requests
        int numberOfRequests = 10;

        // Create an array of tasks
        Task[] tasks = new Task[numberOfRequests];

        for (int i = 0; i < numberOfRequests; i++)
        {
            // Assign each task to a method that makes an API call
            tasks[i] = Task.Run(() => MakeApiCall(i));
        }

        // Await the completion of all tasks
        await Task.WhenAll(tasks);

        Console.WriteLine("All requests completed.");
    }

    static async Task MakeApiCall(int requestId)
    {
        try
        {
            // Replace with your API URL
            string apiUrl = "https://api.example.com/data";
            HttpResponseMessage response = await httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Request {requestId} completed: {responseBody}");
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Request {requestId} error: {e.Message}");
        }
    }
}

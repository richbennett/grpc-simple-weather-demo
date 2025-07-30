using Grpc.Core;
using Grpc.Net.Client;
using WeatherService;

public class Program
{
    public static async Task Main(string[] args)
    {
        using var channel = GrpcChannel.ForAddress("https://localhost:7042");
        var client = new WeatherForecast.WeatherForecastClient(channel);

        Console.WriteLine("=== gRPC Weather Client Demo ===\n");

        try
        {
            Console.ReadKey();
            await DemoUnaryCall(client);
            Console.ReadKey();
            await DemoServerStreaming(client);
            Console.ReadKey();
            await DemoClientStreaming(client);
            Console.ReadKey();
            await DemoBidirectionalStreaming(client);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Make sure the WeatherService is running!");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();


    }

    static async Task DemoUnaryCall(WeatherForecast.WeatherForecastClient client)
    {
        Console.WriteLine("DEMO 1: Unary Call");
        Console.WriteLine("Getting weather for London...\n");

        var response = await client.GetWeatherAsync(new WeatherRequest
        {
            City = "London",
            Days = 1
        });

        Console.WriteLine($"City: {response.City}");
        Console.WriteLine($"Temperature: {response.TemperatureCelsius}°C");
        Console.WriteLine($"Condition: {response.Condition}");
        Console.WriteLine($"Date: {response.Date}");
        Console.WriteLine($"Humidity: {response.Humidity}%");
        Console.WriteLine($"Wind: {response.WindSpeed} km/h");

        Console.WriteLine("\n" + new string('-', 50) + "\n");
    }

    static async Task DemoServerStreaming(WeatherForecast.WeatherForecastClient client)
    {
        Console.WriteLine("DEMO 2: Server Streaming");
        Console.WriteLine("Streaming 5-day forecast for Tokyo...\n");

        using var streamingCall = client.GetWeatherStream(new WeatherRequest
        {
            City = "Tokyo",
            Days = 5
        });

        // Use while loop instead of ReadAllAsync for compatibility
        while (await streamingCall.ResponseStream.MoveNext())
        {
            var response = streamingCall.ResponseStream.Current;
            Console.WriteLine($"{response.Date}: {response.TemperatureCelsius}°C, " +
                             $"{response.Condition} (Humidity: {response.Humidity}%)");

            await Task.Delay(500);
        }

        Console.WriteLine("\n" + new string('-', 50) + "\n");
    }

    static async Task DemoClientStreaming(WeatherForecast.WeatherForecastClient client)
    {
        Console.WriteLine("DEMO 3: Client Streaming");
        Console.WriteLine("Uploading batch weather data...\n");

        using var call = client.UploadWeatherData();

        var cities = new[] { "Paris", "Berlin", "Madrid", "Rome", "Amsterdam" };
        var conditions = new[] { "Sunny", "Cloudy", "Rainy", "Windy", "Foggy" };

        for (int i = 0; i < 10; i++)
        {
            // create some weather data to upload
            var weatherData = new WeatherReply
            {
                City = cities[i % cities.Length],
                TemperatureCelsius = Random.Shared.Next(15, 30),
                Condition = conditions[i % conditions.Length],
                Date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
                Humidity = Random.Shared.Next(40, 80),
                WindSpeed = Random.Shared.Next(5, 25)
            };

            Console.WriteLine($"Uploading: {weatherData.City} - {weatherData.TemperatureCelsius}°C");
            await call.RequestStream.WriteAsync(weatherData);
            await Task.Delay(200);
        }

        // complete/close the request stream
        await call.RequestStream.CompleteAsync();

        // get the return value for the client-streaming call
        var summary = await call;

        Console.WriteLine($"\nUpload Summary:");
        Console.WriteLine($"   Total Records: {summary.TotalRecords}");
        Console.WriteLine($"   Average Temperature: {summary.AverageTemperature:F1}°C");
        Console.WriteLine($"   Most Common Condition: {summary.MostCommonCondition}");

        Console.WriteLine("\n" + new string('-', 50) + "\n");
    }
    
    static async Task DemoBidirectionalStreaming(WeatherForecast.WeatherForecastClient client)
    {
        Console.WriteLine("DEMO 4: Bidirectional Streaming - Interactive Weather Requests and Alerts");

        using var call = client.LiveWeatherUpdates();

        // Background task to handle alerts for requests (these will fire for each message written to the request stream)
        var responseTask = Task.Run(async () =>
        {
            while (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                Console.WriteLine($"ALERT: {response.City} weather changed to {response.TemperatureCelsius}°C, {response.Condition}");
            }
        });

        Console.WriteLine("Benefit: BOTH sides can send data simultaneously over ONE connection");

        // Simulate real-time monitoring requests
        var monitoringRequests = new[]
        {
            ("New York", "User requested current conditions"),
            ("Los Angeles", "Automated weather station check"),
            ("Chicago", "Emergency weather alert system"),
            ("Miami", "Flight planning system request")
        };


        foreach (var (city, reason) in monitoringRequests)
        {
            Console.WriteLine($"REQUEST: {reason} for {city}");
            await call.RequestStream.WriteAsync(new WeatherRequest { City = city, Days = 1 });
            await Task.Delay(2000); // Give time to see the response
        }

        await call.RequestStream.CompleteAsync();
        await responseTask;

        Console.WriteLine("\n" + new string('-', 50) + "\n");
    }
}

using Grpc.Core;

namespace WeatherService.Services;

public class WeatherForecastService : WeatherForecast.WeatherForecastBase
{
    private readonly ILogger<WeatherForecastService> _logger;
    private readonly string[] _conditions = { "Sunny", "Cloudy", "Rainy", "Snowy", "Windy", "Foggy", "Stormy" };
    private readonly Random _random = new();

    public WeatherForecastService(ILogger<WeatherForecastService> logger)
    {
        _logger = logger;
        _logger.Log(LogLevel.Information, "WeatherForecastService instantiated...");
    }

    // Unary call
    public override Task<WeatherReply> GetWeather(WeatherRequest request, ServerCallContext context)
    {
        _logger.LogInformation($"Getting weather for {request.City}");

        var reply = GenerateWeatherReply(request.City);
        return Task.FromResult(reply);
    }

    // Server streaming - great for live data feeds
    public override async Task GetWeatherStream(WeatherRequest request,
        IServerStreamWriter<WeatherReply> responseStream, ServerCallContext context)
    {
        _logger.LogInformation($"Streaming weather for {request.City} for {request.Days} days");

        for (int i = 0; i < request.Days && !context.CancellationToken.IsCancellationRequested; i++)
        {
            var reply = GenerateWeatherReply(request.City, DateTime.Now.AddDays(i));
            await responseStream.WriteAsync(reply);

            // Simulate some processing time
            await Task.Delay(1000, context.CancellationToken);
        }
    }

    // Client streaming - great for batch uploads
    public override async Task<WeatherSummary> UploadWeatherData(
        IAsyncStreamReader<WeatherReply> requestStream, ServerCallContext context)
    {
        _logger.LogInformation("Starting weather data upload");

        var temperatures = new List<int>();
        var conditions = new List<string>();
        int recordCount = 0;

        await foreach (var weatherData in requestStream.ReadAllAsync())
        {
            temperatures.Add(weatherData.TemperatureCelsius);
            conditions.Add(weatherData.Condition);
            recordCount++;

            _logger.LogInformation($"Received weather data for {weatherData.City}: {weatherData.TemperatureCelsius}°C");
        }

        var avgTemp = temperatures.Count > 0 ? temperatures.Average() : 0;
        var mostCommonCondition = conditions
            .GroupBy(c => c)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Unknown";

        return new WeatherSummary
        {
            TotalRecords = recordCount,
            AverageTemperature = avgTemp,
            MostCommonCondition = mostCommonCondition
        };
    }

    // Bidirectional streaming - great for real-time interactive scenarios
    public override async Task LiveWeatherUpdates(
        IAsyncStreamReader<WeatherRequest> requestStream,
        IServerStreamWriter<WeatherReply> responseStream,
        ServerCallContext context)
    {
        _logger.LogInformation("Starting live weather updates");

        await foreach (var request in requestStream.ReadAllAsync())
        {
            _logger.LogInformation($"Live request for {request.City}");

            var reply = GenerateWeatherReply(request.City);
            await responseStream.WriteAsync(reply);
        }
    }

    private WeatherReply GenerateWeatherReply(string city, DateTime? date = null)
    {
        // get some random data...
        return new WeatherReply
        {
            City = city,
            TemperatureCelsius = _random.Next(-20, 45),
            Condition = _conditions[_random.Next(_conditions.Length)],
            Date = (date ?? DateTime.Now).ToString("yyyy-MM-dd"),
            Humidity = Math.Round(_random.NextDouble() * 100, 1),
            WindSpeed = Math.Round(_random.NextDouble() * 50, 1),
            DewPoint = Math.Round(_random.NextDouble() / 100, 1),
        };
    }
}

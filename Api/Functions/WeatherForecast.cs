using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Postit;

namespace Api;

public class WeatherForecast
{
    private readonly ILogger _logger;

    public WeatherForecast(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<WeatherForecast>();
    }

    [Function("WeatherForecast")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.WriteAsJsonAsync(GetForecasts());

        return response;
    }

    private static Forecast[] GetForecasts() => new[]
    {
        new Forecast(DateTime.Parse("2022-01-06"), 1, "Freezing"),
        new Forecast(DateTime.Parse("2022-01-07"), 14, "Bracing"),
        new Forecast(DateTime.Parse("2022-01-08"), -13, "Freezing"),
        new Forecast(DateTime.Parse("2022-01-09"), -16, "Balmy"),
        new Forecast(DateTime.Parse("2022-01-10"), -2, "Chilly")
    };
}

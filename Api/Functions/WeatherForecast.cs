using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
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
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        [CosmosDBInput("Postit", "WeatherForecasts",
            Connection = "CosmosDBConnectionString")] Container container)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var query = container.GetItemLinqQueryable<Forecast>();
        var iterator = query.ToFeedIterator();
        var forecasts = await iterator.ReadNextAsync(req.FunctionContext.CancellationToken);
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(forecasts);
        return response;
    }
}

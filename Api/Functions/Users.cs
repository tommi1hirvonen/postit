using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Postit;

namespace Api;

public class Users
{
    private readonly ILogger _logger;

    public Users(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Users>();
    }

    [Function("Users")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req,
        [CosmosDBInput("Postit", "Users",
            Connection = "CosmosDBConnectionString")] IEnumerable<User> users)
    {
        _logger.LogInformation("Fetching users");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(users);
        return response;
    }
}

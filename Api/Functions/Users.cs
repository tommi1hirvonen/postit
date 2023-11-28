using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Postit.Api;

public class Users
{
    private readonly ILogger _logger;

    public Users(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Users>();
    }

    [Function("Users")]
    public async Task<HttpResponseData> GetUsersAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Users")] HttpRequestData req,
        [CosmosDBInput("Postit", "Users",
            Connection = "CosmosDBConnectionString")] IEnumerable<User> users)
    {
        _logger.LogInformation("Fetching users");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(users);
        return response;
    }

    [Function("User")]
    public async Task<HttpResponseData> GetUserByIdAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "Users/{userId}")] HttpRequestData req,
        [CosmosDBInput("Postit", "Users",
            Connection = "CosmosDBConnectionString",
            Id = "{userId}",
            PartitionKey = "{userId}")] User user)
    {
        _logger.LogInformation("Fetching users");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(user);
        return response;
    }
}

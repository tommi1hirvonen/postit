using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Postit.Api;

public class Posts
{
    private readonly ILogger _logger;

    public Posts(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Posts>();
    }

    [Function("Posts")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "posts/{userId}")] HttpRequestData req,
        [CosmosDBInput("Postit", "Posts",
            Connection = "CosmosDBConnectionString",
            PartitionKey = "{userId}")] IEnumerable<Post> posts)
    {
        _logger.LogInformation("Fetching posts");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(posts);
        return response;
    }
}

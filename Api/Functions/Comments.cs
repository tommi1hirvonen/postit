using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Postit.Api;

public class Comments(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<Comments>();

    [Function("Comments")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "comments/{postId}")] HttpRequestData req,
        [CosmosDBInput("Postit", "Comments",
            Connection = "CosmosDBConnectionString",
            PartitionKey = "{postId}")] IEnumerable<Comment> comments)
    {
        _logger.LogInformation("Fetching comments");
        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(comments);
        return response;
    }
}

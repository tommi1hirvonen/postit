using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Postit;

namespace Api;

public class Search
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public Search(ILoggerFactory loggerFactory, IConfiguration configuration)
    {
        _logger = loggerFactory.CreateLogger<Search>();
        _configuration = configuration;
    }

    [Function("Search")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    {
        var postsQuery = await req.ReadFromJsonAsync<PostsQuery>();
        ArgumentNullException.ThrowIfNull(postsQuery);
        var (usernameSearchTerm, postSearchTerm) = postsQuery;

        var connectionString = _configuration.GetValue<string>("CosmosDBConnectionString");
        ArgumentNullException.ThrowIfNull(connectionString);
        using var client = new CosmosClient(connectionString);
        
        List<Postit.User>? users = null;
        if (usernameSearchTerm.Length > 0)
        {
            usernameSearchTerm = $"%{EncodeForLike(usernameSearchTerm.ToLower())}%";
            var usersQueryDef = new QueryDefinition("SELECT * FROM users u WHERE LOWER(u.username) LIKE @term")
                .WithParameter("@term", usernameSearchTerm);
            var usersContainer = client.GetContainer("Postit", "Users");
            using var usersFeed = usersContainer.GetItemQueryIterator<Postit.User>(usersQueryDef);
            users = new();
            while (usersFeed.HasMoreResults)
            {
                var feedResponse = await usersFeed.ReadNextAsync();
                users.AddRange(feedResponse.AsEnumerable());
            }
        }
        
        var posts = new List<Post>();

        // Users is not empty and no results were found
        if (users?.Count == 0)
        {
            var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
            await emptyResponse.WriteAsJsonAsync(posts);
            return emptyResponse;
        }

        var userIds = users?.Select((u, i) => (u.Id, Index: i)).ToArray();

        var postsContainer = client.GetContainer("Postit", "Posts");
        var postsQueryDef = (postSearchTerm, userIds) switch
        {
            ({ Length: 0 }, null) =>
                new QueryDefinition("SELECT * FROM posts p"),

            ({ Length: 0 }, not null) =>
                userIds.Aggregate(
                    new QueryDefinition($"SELECT * FROM posts p WHERE p.userId IN ({string.Join(',', userIds.Select(i => $"@p{i.Index}"))})"),
                    (q, id) => q.WithParameter($"@p{id.Index}", id.Id)),

            ({ Length: >0 }, null) =>
                new QueryDefinition("SELECT * FROM posts p WHERE LOWER(p.body) LIKE @term")
                    .WithParameter("@term", $"%{EncodeForLike(postSearchTerm.ToLower())}%"),

            ({ Length: >0 }, not null) =>
                userIds.Aggregate(
                    new QueryDefinition($"SELECT * FROM posts p WHERE LOWER(p.body) LIKE @term AND p.userId IN ({string.Join(',', userIds.Select(i => $"@p{i.Index}"))})")
                        .WithParameter("@term", $"%{EncodeForLike(postSearchTerm.ToLower())}%"),
                        (q, id) => q.WithParameter($"@p{id.Index}", id.Id))   
        };
        using var postsFeed = postsContainer.GetItemQueryIterator<Post>(postsQueryDef);
        while (postsFeed.HasMoreResults)
        {
            var feedResponse = await postsFeed.ReadNextAsync();
            posts.AddRange(feedResponse.AsEnumerable());
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(posts);
        return response;
    }

    private static string EncodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
}

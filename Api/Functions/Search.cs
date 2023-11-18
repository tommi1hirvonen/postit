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

        // Users is not null but no results were found => don't bother searching for posts.
        if (users?.Count == 0)
        {
            var emptyResponse = req.CreateResponse(HttpStatusCode.OK);
            await emptyResponse.WriteAsJsonAsync(posts);
            return emptyResponse;
        }

        // Map ids to their index.
        var userIds = users?.Select((u, i) => (u.Id, Index: i)).ToArray();

        var postsContainer = client.GetContainer("Postit", "Posts");
        var postsQueryDef = (postSearchTerm.Length, userIds) switch
        {
            (0, null) => new QueryDefinition("SELECT * FROM posts p"),
            (0, not null) => BuildQueryWithUserFilter(userIds),
            (_, null) => BuildQueryWithPostFilter(postSearchTerm),
            (_, not null) => BuildQueryWithUserAndPostFilter(userIds, postSearchTerm)  
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

    private static QueryDefinition BuildQueryWithUserFilter((string Id, int Index)[] userIds)
    {
        var idParams = string.Join(',', userIds.Select(i => $"@p{i.Index}"));
        var baseQuery = new QueryDefinition($"SELECT * FROM posts p WHERE p.userId IN ({idParams})");
        // Add parameters for user ids.
        var query = userIds.Aggregate(baseQuery, (q, id) => q.WithParameter($"@p{id.Index}", id.Id));
        return query;
    }

    private static QueryDefinition BuildQueryWithPostFilter(string postSearchTerm)
    {
        var searchTerm = $"%{EncodeForLike(postSearchTerm.ToLower())}%";
        var query = new QueryDefinition("SELECT * FROM posts p WHERE LOWER(p.body) LIKE @term")
            .WithParameter("@term", searchTerm);
        return query;
    }

    private static QueryDefinition BuildQueryWithUserAndPostFilter((string Id, int Index)[] userIds, string postSearchTerm)
    {
        var idParams = string.Join(',', userIds.Select(i => $"@p{i.Index}"));
        var searchTerm = $"%{EncodeForLike(postSearchTerm.ToLower())}%";
        var baseQuery = new QueryDefinition($"SELECT * FROM posts p WHERE LOWER(p.body) LIKE @term AND p.userId IN ({idParams})")
            .WithParameter("@term", searchTerm);
        // Add parameters for user ids.
        var query = userIds.Aggregate(baseQuery, (q, id) => q.WithParameter($"@p{id.Index}", id.Id));
        return query;
    }

    private static string EncodeForLike(string term) => term.Replace("[", "[[]").Replace("%", "[%]");
}

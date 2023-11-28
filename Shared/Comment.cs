namespace Postit.Models;

public record Comment(string Id, string PostId, string Name, string Email, string Body);
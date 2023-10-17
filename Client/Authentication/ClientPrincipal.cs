namespace Postit.Authentication;

public record ClientPrincipal(string IdentityProvider, string UserId, string UserDetails)
{
    public IEnumerable<string> UserRoles { get; set; } = Enumerable.Empty<string>();
}
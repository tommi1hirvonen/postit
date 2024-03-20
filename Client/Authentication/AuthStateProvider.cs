using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
namespace Postit.Client.Authentication;

public class AuthStateProvider(IHttpClientFactory httpClientFactory) : AuthenticationStateProvider
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("host");
    private static readonly IEnumerable<string> Anonymous = ["anonymous"];

    public async override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<AuthData>("/.auth/me");
            ArgumentNullException.ThrowIfNull(data);
            var principal = data.ClientPrincipal;
            principal.UserRoles = principal.UserRoles.Except(Anonymous, StringComparer.CurrentCultureIgnoreCase);

            if (!principal.UserRoles.Any())
            {
                return new AuthenticationState(new ClaimsPrincipal());
            }

            var identity = new ClaimsIdentity(principal.IdentityProvider);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, principal.UserId));
            identity.AddClaim(new Claim(ClaimTypes.Name, principal.UserDetails));
            identity.AddClaims(principal.UserRoles.Select(r => new Claim(ClaimTypes.Role, r)));
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return new AuthenticationState(new ClaimsPrincipal());
        }
    }
}
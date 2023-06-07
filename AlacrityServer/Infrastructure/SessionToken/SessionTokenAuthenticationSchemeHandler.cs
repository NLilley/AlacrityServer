using AlacrityCore.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AlacrityServer.Infrastructure.SessionToken;

public class SessionTokenAuthenticationSchemeOptions : AuthenticationSchemeOptions { };

public class SessionTokenAuthenticationSchemeHandler : AuthenticationHandler<SessionTokenAuthenticationSchemeOptions>
{
    public SessionTokenAuthenticationSchemeHandler(
        IOptionsMonitor<SessionTokenAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    ) : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
            return AuthenticateResult.NoResult();

        var session = Context.Session;

        await session.LoadAsync();
        var isAuthenticated = session.GetInt32(SessionUtil.IsAuthenticatedKey);
        if (isAuthenticated != 1)
        {
            if (!isAuthenticated.HasValue)
            {
                session.SetInt32(SessionUtil.IsAuthenticatedKey, 0);
                await session.CommitAsync();
            }

            return AuthenticateResult.Fail("User is not authenticated");
        }

        var claims = Array.Empty<Claim>();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}

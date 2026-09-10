using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using EasyPanel.Backend.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EasyPanel.Backend.Infrastructure.Security;

/// <summary>
/// Authenticates a daemon's SignalR connection by node token, not user JWT. The daemon
/// connects with `?nodeId={id}` in the hub URL and supplies the raw token as SignalR's
/// AccessTokenProvider — SignalR puts that on the `access_token` query string for
/// WebSocket connections, since browsers/clients can't set arbitrary headers on the
/// WebSocket handshake. We hash the presented token and compare against the stored hash;
/// we never store or compare the raw token itself.
/// </summary>
public sealed class NodeTokenAuthenticationHandler(
    IOptionsMonitor<NodeTokenAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    AppDbContext dbContext)
    : AuthenticationHandler<NodeTokenAuthenticationOptions>(options, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Guid.TryParse(Request.Query["nodeId"].ToString(), out var nodeId))
        {
            return AuthenticateResult.Fail("Missing or invalid 'nodeId' query parameter.");
        }

        var rawToken = ExtractRawToken();
        if (string.IsNullOrEmpty(rawToken))
        {
            return AuthenticateResult.Fail("Missing node token.");
        }

        var node = await dbContext.Nodes.SingleOrDefaultAsync(candidate => candidate.Id == nodeId);
        if (node is null)
        {
            return AuthenticateResult.Fail("Unknown node.");
        }

        var presentedTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        var hashesMatch = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(presentedTokenHash),
            Encoding.UTF8.GetBytes(node.NodeTokenHash));

        if (!hashesMatch)
        {
            return AuthenticateResult.Fail("Invalid node token.");
        }

        var identity = new ClaimsIdentity(
            [new Claim(NodeClaimTypes.NodeId, node.Id.ToString())],
            Scheme.Name);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private string? ExtractRawToken()
    {
        var accessTokenFromQuery = Request.Query["access_token"].ToString();
        if (!string.IsNullOrEmpty(accessTokenFromQuery))
        {
            return accessTokenFromQuery;
        }

        var authorizationHeader = Request.Headers.Authorization.ToString();
        return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..]
            : null;
    }
}

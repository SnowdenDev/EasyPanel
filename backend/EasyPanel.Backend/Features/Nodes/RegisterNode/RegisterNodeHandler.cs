using System.Security.Cryptography;
using EasyPanel.Backend.Infrastructure;
using EasyPanel.Backend.Infrastructure.Entities;

namespace EasyPanel.Backend.Features.Nodes.RegisterNode;

public sealed class RegisterNodeHandler(AppDbContext dbContext)
{
    private const int RawTokenSizeInBytes = 32;

    public async Task<RegisterNodeResponse> HandleAsync(RegisterNodeRequest request, CancellationToken cancellationToken)
    {
        var rawTokenBytes = RandomNumberGenerator.GetBytes(RawTokenSizeInBytes);
        var rawToken = Convert.ToHexString(rawTokenBytes);
        var tokenHash = Convert.ToHexString(SHA256.HashData(rawTokenBytes));

        var node = new Node
        {
            Id = Guid.NewGuid(),
            DisplayName = request.DisplayName,
            NodeTokenHash = tokenHash,
            ConnectivityMode = request.ConnectivityMode,
            IsOnline = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        dbContext.Nodes.Add(node);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterNodeResponse(node.Id, rawToken, node.ConnectivityMode);
    }
}

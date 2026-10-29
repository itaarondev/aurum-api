using Aurum.Domain.Entities;

namespace Aurum.Application.Common.Interfaces
{
    public interface IRefreshTokenService
    {
        Task SaveTokenRefreshAsync(string userId, string refreshToken, CancellationToken cancellationToken);
        Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
        Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

        Task RevokeAllTokensForUserAsync(string userId, CancellationToken cancellationToken);
    }
}

using Aurum.Application.Common.Interfaces;
using Aurum.Domain.Entities;
using Aurum.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Aurum.Persistence.Services
{
    internal class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        public RefreshTokenService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
        {
            return await _context.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
        }

        public async Task UpdateAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            _context.Set<RefreshToken>().Update(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task SaveTokenRefreshAsync(string userId, string refreshToken, CancellationToken cancellationToken)
        {
            int expirationDays = int.TryParse(_configuration["JwtSettings:RefreshTokenExpirationDays"], out var days) ? days : 7;
            var model = new RefreshToken
            {
                UserId = userId,
                Token = refreshToken,
                ExpiresOnUtc = DateTime.UtcNow.AddDays(expirationDays),
                CreatedOnUtc = DateTime.UtcNow
            };
            await _context.Set<RefreshToken>().AddAsync(model, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RevokeAllTokensForUserAsync(string userId, CancellationToken cancellationToken)
        {
            var activeTokens = await _context.Set<RefreshToken>().Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

namespace Aurum.Application.Common.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(string userId, string email, IList<string> roles);
        string GenerateRefreshToken();
    }
}

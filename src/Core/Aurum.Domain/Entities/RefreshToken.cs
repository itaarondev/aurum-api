namespace Aurum.Domain.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresOnUtc { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresOnUtc;
        public bool IsRevoked { get; set; }
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}

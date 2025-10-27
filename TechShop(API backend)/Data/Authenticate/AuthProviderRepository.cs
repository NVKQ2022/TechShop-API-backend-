using Microsoft.EntityFrameworkCore;
using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Models.Authenticate;

namespace TechShop_API_backend_.Data.Authenticate
{
    public class AuthProviderRepository
    {
        private readonly AuthenticateDbContext _context;

        public AuthProviderRepository(AuthenticateDbContext context)
        {
            _context = context;
        }

        // ✅ Get provider by provider type + provider user ID (e.g., Google, Facebook)
        public async Task<AuthProvider?> GetByProviderAsync(string provider, string providerUserId)
        {
            return await _context.AuthProviders
                .Include(a => a.User)
                .FirstOrDefaultAsync(a =>
                    a.Provider == provider &&
                    a.ProviderUserId == providerUserId);
        }

        // ✅ Get provider by user ID
        public async Task<AuthProvider?> GetByUserIdAsync(int userId)
        {
            return await _context.AuthProviders
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        // ✅ Add new provider
        public async Task<AuthProvider> AddAsync(AuthProvider provider)
        {
            _context.AuthProviders.Add(provider);
            await _context.SaveChangesAsync();
            return provider;
        }

        // ✅ Update access/refresh tokens or expiry
        public async Task<bool> UpdateTokensAsync(Guid providerId, string? accessToken, string? refreshToken, DateTime? tokenExpiresAt)
        {
            var existing = await _context.AuthProviders.FirstOrDefaultAsync(a => a.Id == providerId);
            if (existing == null)
                return false;

            existing.AccessToken = accessToken ?? existing.AccessToken;
            existing.RefreshToken = refreshToken ?? existing.RefreshToken;
            existing.TokenExpiresAt = tokenExpiresAt ?? existing.TokenExpiresAt;
            existing.UpdatedAt = DateTime.UtcNow;

            _context.AuthProviders.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ Delete provider (optional)
        public async Task<bool> DeleteAsync(Guid providerId)
        {
            var existing = await _context.AuthProviders.FirstOrDefaultAsync(a => a.Id == providerId);
            if (existing == null)
                return false;

            _context.AuthProviders.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

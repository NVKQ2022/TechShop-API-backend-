using System;
using TechShop_API_backend_.Models.Authenticate;
using TechShop_API_backend_.Data.Context;
using Microsoft.EntityFrameworkCore;


namespace TechShop_API_backend_.Data.Authenticate
{
    public class UserFcmRepository
    {
        private readonly AuthenticateDbContext _context;

        public UserFcmRepository(AuthenticateDbContext context)
        {
            _context = context;
        }

        // 🔍 Get token by userId
        public async Task<UserFcm?> GetByUserIdAsync(int userId)
        {
            return await _context.UserFcm
                .FirstOrDefaultAsync(f => f.UserId == userId);
        }

        // 🔄 Create or update FCM token
        public async Task<UserFcm> UpsertTokenAsync(int userId, string fcmToken)
        {
            var existing = await _context.UserFcm.FindAsync(userId);

            if (existing == null)
            {
                var newRow = new UserFcm
                {
                    UserId = userId,
                    FcmToken = fcmToken
                };

                _context.UserFcm.Add(newRow);

                await _context.SaveChangesAsync();
                return newRow;
            }

            existing.FcmToken = fcmToken;
            await _context.SaveChangesAsync();
            return existing;
        }

        // ❌ Remove token when user logs out (optional)
        public async Task<bool> DeleteByUserIdAsync(int userId)
        {
            var row = await _context.UserFcm.FindAsync(userId);
            if (row == null)
                return false;

            _context.UserFcm.Remove(row);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

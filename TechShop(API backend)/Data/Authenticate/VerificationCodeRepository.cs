using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TechShop.API.Models;
using TechShop_API_backend_.Data.Context;

namespace TechShop.API.Repositories
{
    public class VerificationCodeRepository
    {
        private readonly AuthenticateDbContext _context;

        public VerificationCodeRepository(AuthenticateDbContext context)
        {
            _context = context;
        }

        // Create new verification code
        public async Task<VerificationCode> CreateAsync(VerificationCode code)
        {
            _context.Set<VerificationCode>().Add(code);
            await _context.SaveChangesAsync();
            return code;
        }

        // Get by code (e.g., token lookup)
        public async Task<VerificationCode> GetByCodeAsync(string code)
        {
            return await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v => v.Code == code);
        }

        // Get latest unused code by email and type
        public async Task<VerificationCode> GetLatestAsync(string email, string type)
        {
            return await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Type == type &&
                    !v.IsUsed &&
                    v.ExpiresAt > DateTime.Now);
        }

        // Verify a code
        public async Task<bool> VerifyAsync(string email, string type, string code)
        {
            var verification = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Type == type &&
                    v.Code == code &&
                    !v.IsUsed &&
                    v.ExpiresAt > DateTime.Now);

            if (verification == null)
                return false;

            verification.IsUsed = true;
            verification.UsedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }



        public async Task<bool> VerifyAsync(int userId, string type, string code)
        {
            var verification = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.UserId == userId &&
                    v.Type == type &&
                    v.Code == code &&
                    !v.IsUsed &&
                    v.ExpiresAt > DateTime.Now);

            if (verification == null)
                return false;

            verification.IsUsed = true;
            verification.UsedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        // Delete expired codes (optional cleanup)
        public async Task<int> DeleteExpiredAsync()
        {
            var expired = await _context.Set<VerificationCode>()
                .Where(v => v.ExpiresAt < DateTime.Now)
                .ToListAsync();

            _context.Set<VerificationCode>().RemoveRange(expired);
            return await _context.SaveChangesAsync();
        }
    }
}

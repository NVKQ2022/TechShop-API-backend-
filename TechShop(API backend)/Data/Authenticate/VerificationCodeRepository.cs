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


        public async Task<VerificationCode> GetVerificationByEmailAsync(string email)
        {
            return await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v => v.Email == email);
        }

        public async Task<VerificationCode> GetVerificationByTypeAsync(string type)
        {
            return await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v => v.Type == type);
        }

        public async Task<VerificationCode> GetVerificationByEmailNotExpireAsync(string email)
        {
            return await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v => v.Email == email && v.ExpiresAt > DateTime.Now);
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

        // Check if a verification code is used, and mark it as used if not
        public async Task<bool> IsVerifyCodeUsed(string email, string type, string code)
        {
            var verification = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Type == type &&
                    v.Code == code);

            if (verification == null)
            {
                // If no verification code is found, it means it doesn't exist.
                return false;
            }

            // Return whether the code has already been used or not
            return verification.IsUsed;
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


        // Delete expired codes
        public async Task<int> DeleteExpiredAsync()
        {
            var expiredCodes = await _context.Set<VerificationCode>()
                .Where(v => v.ExpiresAt <= DateTime.Now)
                .ToListAsync();
            _context.Set<VerificationCode>().RemoveRange(expiredCodes);
            return await _context.SaveChangesAsync();
        }

        // Delete 
        public async Task<bool> DeleteAsync(string email, string type, string code)
        {
            var verification = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Type == type &&
                    v.Code == code);
            if (verification == null)
                return false;
            _context.Set<VerificationCode>().Remove(verification);
            await _context.SaveChangesAsync();
            return true;
        }

    }
}

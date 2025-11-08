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


        public async Task<bool> IsVerifyCodeUsed(string email, string type, string code)
        {
            var verification = await _context.Set<VerificationCode>()
                .FirstOrDefaultAsync(v =>
                    v.Email == email &&
                    v.Type == type &&
                    v.Code == code &&
                    !v.IsUsed &&  // Ensure the code is not used yet
                    v.ExpiresAt > DateTime.Now); // Ensure the code hasn't expired

            if (verification == null)
            {
                // If no verification code is found, it means either it is expired, already used, or doesn't exist.
                return false;
            }

            // If verification exists and is valid (not used and not expired), mark it as used.
            verification.IsUsed = true;
            await _context.SaveChangesAsync();  // Save the changes to mark it as used

            return true; // The code was not used before and is valid
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



        
    }
}

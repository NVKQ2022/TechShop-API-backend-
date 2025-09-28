using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models;
using Microsoft.EntityFrameworkCore;


namespace TechShop_API_backend_.Data
{
    public class UserRepository
    {
       
            private readonly AccountDbContext _context;

            public UserRepository(AccountDbContext context)
            {
                _context = context;
            }

            // CREATE
            public async Task<User> CreateUserAsync(string email, string username, string password, bool isAdmin = false)
            {
                // Generate salt
                var salt = SecurityHelper.GenerateSalt();
                var hashedPassword = SecurityHelper.HashPassword(password, salt);

                var user = new User
                {
                    Email = email,
                    Username = username,
                    Password = hashedPassword,
                    Salt = salt,
                    IsAdmin = isAdmin
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return user;
            }

            // READ
            public async Task<User?> GetUserByIdAsync(int id)
            {
                return await _context.Users.FindAsync(id);
            }

            public async Task<User?> GetUserByUsernameAsync(string username)
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            }
            
            public async Task<List<User>> GetAllUsersAsync()
            {
            return await _context.Users.ToListAsync();
            }
            // UPDATE
            public async Task<bool> UpdateUserAsync(User user)  // ???
            {
                _context.Users.Update(user);
                return await _context.SaveChangesAsync() > 0;
            }

            // DELETE
            public async Task<bool> DeleteUserAsync(int id)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null) return false;

                _context.Users.Remove(user);
                return await _context.SaveChangesAsync() > 0;
            }

            public async Task<bool> DeleteUserByNameAsync(string Username)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == Username);
            if (user == null) return false;

                _context.Users.Remove(user);
                return await _context.SaveChangesAsync() > 0;
            }



        // PASSWORD CHECK
        public bool VerifyPassword(User user, string password)
            {
                var hash = SecurityHelper.HashPassword(password, user.Salt);
                return hash == user.Password;
            }


        }
    
}

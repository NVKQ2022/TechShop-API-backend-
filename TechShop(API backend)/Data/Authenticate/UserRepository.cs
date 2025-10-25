using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MongoDB.Driver.Core.Configuration;
using TechShop_API_backend_.Models.Authenticate;


namespace TechShop_API_backend_.Data.Authenticate
{
    public class UserRepository
    {

        private readonly AuthenticateDbContext _context;
        private readonly AuthenticationRepository authenticationRepository;
        private readonly UserDetailRepository userDetailRepository;
        private static readonly string _connectionString = Environment.GetEnvironmentVariable("ConnectionString__UserDatabase") ?? throw new InvalidOperationException("Database connection string not configured");

        public UserRepository(AuthenticateDbContext context, AuthenticationRepository authenticationRepository, UserDetailRepository userDetailRepository)
        {
            _context = context;
            this.authenticationRepository = authenticationRepository;
            this.userDetailRepository = userDetailRepository;
        }



        public async Task<(bool Success, string ErrorMessage, User? CreatedUser)> CreateUserAsync(
    string email, string username, string password, bool isAdmin = false, bool isEmailVerified = false)
        {
            // 1. Check duplicates (app-level validation for nice UX)
            if (await _context.Users.AnyAsync(u => u.Email == email))
                return (false, "Email already exists", null);

            if (await _context.Users.AnyAsync(u => u.Username == username))
                return (false, "Username already exists", null);

            // 2. Prepare user data
            var salt = SecurityHelper.GenerateSalt();
            var hashedPassword = SecurityHelper.HashPassword(password, salt);
            var newId = await AssignIdAsync();

            var user = new User
            {
                Id = newId,
                Email = email,
                Username = username,
                Password = hashedPassword,
                Salt = salt,
                IsAdmin = isAdmin,
                IsEmailVerified = isEmailVerified // For future email verification feature

            };

            var userDetail = new UserDetail
            {
                UserId = newId,
                Avatar = string.Empty,
                Category = new Dictionary<string, int>(),
                Cart = new List<CartItem>(),
                ReceiveInfo = new List<ReceiveInfo>(),
                PhoneNumber = string.Empty,
                Gender = string.Empty,
                Birthday = DateTime.MinValue,
                Banking = new Banking()
            };

            // 3. Save to DB
            try
            {
                _context.Users.Add(user);
                await userDetailRepository.AddUserDetailAsync(userDetail);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Safety net: In case a duplicate slipped through due to race condition
                if (ex.InnerException?.Message.Contains("UQ_EMAIL") == true)
                    return (false, "Email already exists", null);

                if (ex.InnerException?.Message.Contains("UQ_USERNAME") == true)
                    return (false, "Username already exists", null);

                throw; // rethrow if it's another kind of error
            }

            return (true, string.Empty, user);
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
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

        //ID ASSIGNMENT

        public async Task<int> AssignIdAsync()
        {
            int currentId = 0;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Step 1: Read current ID
                var selectCommand = new SqlCommand("SELECT ID FROM userId", connection, transaction);
                var result = await selectCommand.ExecuteScalarAsync();
                currentId = (int)result;

                // Step 2: Increment ID
                var updateCommand = new SqlCommand("UPDATE userId SET ID = @newId", connection, transaction);
                updateCommand.Parameters.AddWithValue("@newId", currentId + 1);
                await updateCommand.ExecuteNonQueryAsync();

                // Commit transaction
                await transaction.CommitAsync();

                return currentId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }






    }


}

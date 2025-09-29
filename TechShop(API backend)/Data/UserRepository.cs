using TechShop_API_backend_.Data.Context;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using MongoDB.Driver.Core.Configuration;


namespace TechShop_API_backend_.Data
{
    public class UserRepository
    {

        private readonly AccountDbContext _context;
        private readonly AuthenticationRepository authenticationRepository;
        private readonly UserDetailRepository userDetailRepository;
        private static readonly string _connectionString = Environment.GetEnvironmentVariable("ConnectionString__UserDatabase") ?? throw new InvalidOperationException("Database connection string not configured");

        public UserRepository(AccountDbContext context, AuthenticationRepository authenticationRepository, UserDetailRepository userDetailRepository)
        {
            _context = context;
            this.authenticationRepository = authenticationRepository;
            this.userDetailRepository = userDetailRepository;
        }

        // CREATE
        public async Task<User> CreateUserAsync(string email, string username, string password, bool isAdmin = false)
        {
            // Generate salt
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
                IsAdmin = isAdmin
            };
            var userDetail = new UserDetail
            {
                UserId = newId,
                Avatar = String.Empty,
                Category = new Dictionary<string, int>(),
                Cart = new List<CartItem>(),
                ReceiveInfo = new List<ReceiveInfo>(),
                PhoneNumber = string.Empty,
                Gender = string.Empty,
                Birthday = new DateTime(),
                Banking = new Banking()
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

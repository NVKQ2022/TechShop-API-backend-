using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Interfaces
{
    public interface IUserRepository
    {
        void AddUser(User user);
        User? AuthenticateUser(string email, string password);
        User? GetUserById(int id);
        bool AdminAuthorize(string userId);
        int GetCurrentUserId();

        bool UpdateCurrentUserId();
        Task<string?> GetSaltByEmailAsync(string email);
    }

}

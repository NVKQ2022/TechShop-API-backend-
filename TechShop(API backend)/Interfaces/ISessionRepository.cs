using TechShop_API_backend_.Models.Authenticate;
namespace TechShop_API_backend_.Interfaces
{
    public interface ISessionRepository
    {
        string CreateSession(User user, string ipAddress, DateTime requestTime, string userAgent);
        //string RetrieveIdFromSession(string sessionId);
        //bool  RetrieveIsAdminFromSession(string sessionId);

        User? RetrieveFromSession(string? sessionId);

        void DeleteSession(string sessionId);
    }
}

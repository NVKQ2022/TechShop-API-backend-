using MongoDB.Driver;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Interfaces
{
    public interface IReviewRepository
    {
        Task CreateReviewAsync(Review review);


        Task<List<Review>> GetReviewsByProductIdAsync(string productId);



        Task<List<Review>> GetReviewsByUserIdAsync(int userId);
        
    }
}

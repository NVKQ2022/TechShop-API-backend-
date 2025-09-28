using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Interfaces
{
    public interface ICategoryRepository
    {
        Task InsertCategoryAsync(string categoryName);
        Task<List<Category>> GetCategoriesSortedByBuyTimeAsync();
    }
}

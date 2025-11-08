using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.DTOs.Order
{
    public class CreateOrderRequest
    {
        public List<Item> Items { get; set; }
        
    }
    public class Item
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

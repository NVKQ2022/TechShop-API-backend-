using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.DTOs.Order
{
    public class OrderCreateRequest
    {
        public List<OrderItem> Items { get; set; }
        public string PaymentMethod { get; set; }
        public ReceiveInfo ReceiveInfo { get; set; }
    }
}

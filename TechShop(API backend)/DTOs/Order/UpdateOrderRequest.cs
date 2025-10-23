using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.DTOs.Order
{
    public class UpdateOrderRequest
    {
        public List<UpdateOrderItem> Items { get; set; }
        public ReceiveInfo ReceiveInfo { get; set; }
    }

    public class UpdateOrderItem
    {
        public string ProductID { get; set; }
        public int Quantity { get; set; }
    }
}

using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.DTOs.Order
{
    public class ConfirmOrderRequest
    {
        public ReceiveInfo ReceiveInfo { get; set; }
        public string PaymentMethod { get; set; }
    }
}

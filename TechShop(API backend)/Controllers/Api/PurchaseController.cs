using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.DTOs.Order;
namespace TechShop_API_backend_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseController : ControllerBase
    {

        UserDetailRepository _detailRepository;
        ProductRepository _productRepository;
        OrderRepository _orderRepository;

        public PurchaseController(UserDetailRepository detailRepository, ProductRepository productRepository, OrderRepository orderRepository)
        {
            _detailRepository = detailRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;

        }





        [HttpPost("confirm/{orderId}")]
        public async Task<IActionResult> ConfirmOrder(string orderId, [FromBody] ConfirmOrderRequest request)
        {
            // Check if the order exists in the database
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return NotFound($"Order with ID {orderId} not found.");
            }

            // Check if the order is already confirmed or processed
            if (order.Status == "Confirmed")
            {
                return BadRequest("Order has already been confirmed.");
            }

            // Validate the ReceiveInfo and PaymentMethod provided in the request
            if (request.ReceiveInfo == null ||
                string.IsNullOrEmpty(request.ReceiveInfo.Name) ||
                string.IsNullOrEmpty(request.ReceiveInfo.Phone) ||
                string.IsNullOrEmpty(request.ReceiveInfo.Address))
            {
                return BadRequest("ReceiveInfo must contain valid Name, Phone, and Address.");
            }

            if (string.IsNullOrEmpty(request.PaymentMethod))
            {
                return BadRequest("PaymentMethod is required.");
            }

            // Assign the received info and payment method to the order
            order.ReceiveInfo = request.ReceiveInfo;
            order.PaymentMethod = request.PaymentMethod;

            // 1. Check if the stock is still available for each item in the order
            foreach (var orderItem in order.Items)
            {
                bool isStockAvailable = await _productRepository.CheckProductStockAsync(orderItem.ProductID, orderItem.Quantity);

                if (!isStockAvailable)
                {
                    return BadRequest($"Insufficient stock for product ID: {orderItem.ProductID}. Cannot confirm the order.");
                }
            }

            // 2. Update the stock in the product repository
            foreach (var orderItem in order.Items)
            {
                bool stockUpdated = await _productRepository.DecreaseProductStockAsync(orderItem.ProductID, orderItem.Quantity);

                if (!stockUpdated)
                {
                    return BadRequest($"Failed to update stock for product ID {orderItem.ProductID}. Order confirmation failed.");
                }
            }

            // 3. Update order status to 'Confirmed'
            order.Status = "Confirmed";  // Change status to Confirmed
            order.CreatedAt = DateTime.UtcNow; // Add confirmation timestamp

            // 4. Save the updated order
            await _orderRepository.UpdateOrderAsync(order);

            // Optionally, you can trigger other actions such as notifying the user or sending a receipt

            return Ok(new { message = "Order confirmed successfully.", order });
        }




    }
}

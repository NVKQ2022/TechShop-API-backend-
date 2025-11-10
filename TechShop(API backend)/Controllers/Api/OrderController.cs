using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Order;
using TechShop_API_backend_.Helpers;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        OrderRepository _orderRepository;
        ProductRepository _productRepository;
        ConverterHelper converterHelper;
        public OrderController(OrderRepository orderRepository, ProductRepository productRepository, ConverterHelper converterHelper)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
            this.converterHelper = converterHelper;
        }



        // ✅ Get all orders (Admin only)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderRepository.GetAllOrdersAsync();
            return Ok(orders);
        }

        // ✅ Get all orders for the logged-in user
        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            var orders = await _orderRepository.GetOrdersByUserAsync(userId);
            if (orders == null || orders.Count == 0)
                return NotFound("You have no orders.");

            return Ok(orders);
        }

        // GET api/<ValuesController>/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(string id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }


        //// POST api/<ValuesController>



        [HttpPost("prepare")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            if (request == null || request.Items == null || request.Items.Count == 0)
                return BadRequest("Invalid order data.");

            int totalCalculatedAmount = 0;
            List<OrderItem> orderItemsVerify = new List<OrderItem>();

            // Use a normal foreach loop instead of ForEach to allow async/await
            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                {
                    return BadRequest("Item quantity must be greater than 0 and price cannot be negative.");
                }

                // Check stock availability asynchronously
                var isStockAvailable = await _productRepository.CheckProductStockAsync(item.ProductId, item.Quantity);
                if (!isStockAvailable)
                {
                    return BadRequest($"Insufficient stock for product ID: {item.ProductId}");
                }

                // Get product details
                Product product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    return BadRequest($"Product with ID {item.ProductId} not found.");
                }

                // Add the product to the order items after converting it
                orderItemsVerify.Add(converterHelper.ConvertProductToOrderItem(product, item.Quantity));

                // Calculate total amount
                totalCalculatedAmount += product.Price * item.Quantity;
            }

            var newOrder = new Order
            {
                UserID = userId,
                Items = orderItemsVerify,
                TotalAmount = totalCalculatedAmount,
                Status = "NotConfirm",
                CreatedAt = DateTime.UtcNow,
            };

            // Save the order to the database
            await _orderRepository.CreateOrderAsync(newOrder);

            return Ok(new { message = "Order waiting to be confirmed.", order = newOrder });
        }





        [HttpPut("status/{orderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(string orderId, [FromBody] string newStatus)
        {
            var success = await _orderRepository.UpdateOrderStatusAsync(orderId, newStatus);
            if (!success)
                return NotFound("Order not found.");
            return Ok($"Order status updated to '{newStatus}'.");
        }

        // ✅ Delete order (admin use)
        [HttpDelete("Admin/{orderId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteOrder(string orderId)
        {
            var success = await _orderRepository.DeleteOrderAsync(orderId);
            if (!success)
                return NotFound("Order not found or already deleted.");
            return Ok("Order deleted successfully.");
        }


        [HttpDelete("{orderId}")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            // Validate user ID
            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            // Get the order
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound("Order not found.");

            // User can only cancel their own order
            if (order.UserID != userId)
                return Forbid("You are not allowed to cancel this order.");

            // Only allow cancellation if status is "Pending" or "NotConfirm"
            if (order.Status != "Pending" && order.Status != "NotConfirm")
                return BadRequest("Only pending or not confirmed orders can be cancelled.");
            var success = await _orderRepository.CancelOrder(orderId);
            if (!success)
                return StatusCode(500, "Failed to cancel the order.");
            return Ok("Order cancelled successfully.");
        }



        //need to review this update order method

        //// ✅ Update an existing order (only if status == "Pending")
        //[Authorize]
        //[HttpPut("update/{orderId}")]
        //public async Task<IActionResult> UpdateOrder(string orderId, [FromBody] UpdateOrderRequest request)
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userIdClaim))
        //        return Unauthorized("User ID not found in token.");

        //    if (!int.TryParse(userIdClaim, out int userId))
        //        return BadRequest("Invalid user ID format.");

        //    var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId);
        //    if (existingOrder == null)
        //        return NotFound("Order not found.");

        //    // User can only modify their own order
        //    if (existingOrder.UserID != userId)
        //        return Forbid("You are not allowed to modify this order.");

        //    // Only allow update if still pending
        //    if (existingOrder.Status != "Pending")
        //        return BadRequest("Order cannot be modified after it is processed or shipped.");

        //    if (request == null || request.Items == null || request.Items.Count == 0)
        //        return BadRequest("Invalid order data.");

        //    int totalCalculatedAmount = 0;
        //    var orderItemsVerify = new List<OrderItem>();

        //    // Rebuild and verify all order items
        //    foreach (var item in request.Items)
        //    {
        //        if (item.Quantity <= 0 || item.UnitPrice < 0)
        //            return BadRequest("Item quantity must be greater than 0 and price cannot be negative.");

        //        var product = await _productRepository.GetByIdAsync(item.ProductID);
        //        if (product == null)
        //            return BadRequest($"Product with ID {item.ProductID} not found.");

        //        // Ensure stock availability
        //        if (product.Stock < item.Quantity)
        //            return BadRequest($"Insufficient stock for product: {product.Name}");

        //        // Update stock (restore old stock first, then decrease again below)
        //        //await _productRepository.RestoreProductStockAsync(item.ProductID, item.Quantity);
        //        await _productRepository.IncreaseProductStockAsync(item.ProductID, existingOrder.Items.FirstOrDefault(i => i.ProductID == item.ProductID)?.Quantity ?? 0);
        //        await _productRepository.DecreaseProductStockAsync(item.ProductID, item.Quantity);

        //        // Add to verified list
        //        var orderItem = converterHelper.ConvertProductToOrderItem(product, item.Quantity);
        //        orderItemsVerify.Add(orderItem);

        //        totalCalculatedAmount += product.Price * item.Quantity;
        //    }

        //    // Update existing order
        //    existingOrder.Items = orderItemsVerify;
        //    existingOrder.TotalAmount = totalCalculatedAmount;
        //    existingOrder.PaymentMethod = request.PaymentMethod;
        //    existingOrder.ReceiveInfo = request.ReceiveInfo;
        //    existingOrder.Status = "Pending"; // reset status when user updates
        //    existingOrder.CreatedAt = DateTime.UtcNow;

        //    await _orderRepository.UpdateOrderAsync(existingOrder);

        //    return Ok(new { message = "Order updated successfully.", order = existingOrder });
        //}





        // ChatGPT revised update order method
        [Authorize]
        [HttpPut("update/{orderId}")]
        public async Task<IActionResult> UpdateOrder(string orderId, [FromBody] UpdateOrderRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");

            var existingOrder = await _orderRepository.GetOrderByIdAsync(orderId);
            if (existingOrder == null)
                return NotFound("Order not found.");

            if (existingOrder.UserID != userId)
                return Forbid("You are not allowed to modify this order.");

            if (existingOrder.Status != "Pending")
                return BadRequest("Order cannot be modified after it is processed or shipped.");

            if (request?.Items == null || request.Items.Count == 0)
                return BadRequest("Invalid order data.");

            // Batch fetch all products in the request
            var productIds = request.Items.Select(i => i.ProductID).ToList();
            var products = await _productRepository.GetByIdsAsync(productIds);
            var productDict = products.ToDictionary(p => p.ProductId, p => p);

            var orderItemsVerify = new List<OrderItem>();
            int totalCalculatedAmount = 0;

            foreach (var item in request.Items)
            {
                if (item.Quantity <= 0)
                    return BadRequest("Item quantity must be greater than 0.");

                if (!productDict.TryGetValue(item.ProductID, out var product))
                    return BadRequest($"Product with ID {item.ProductID} not found.");

                // Restore previous stock if the product was in the existing order
                var previousQuantity = existingOrder.Items.FirstOrDefault(i => i.ProductID == item.ProductID)?.Quantity ?? 0;
                if (previousQuantity > 0)
                    await _productRepository.IncreaseProductStockAsync(item.ProductID, previousQuantity);

                // Ensure enough stock
                if (product.Stock < item.Quantity)
                    return BadRequest($"Insufficient stock for product: {product.Name}");

                // Decrease stock for new quantity
                await _productRepository.DecreaseProductStockAsync(item.ProductID, item.Quantity);

                // Build order item (preserve historical price if provided)
                var orderItem = converterHelper.ConvertProductToOrderItem(product, item.Quantity);
                orderItem.UnitPrice = item.UnitPrice > 0 ? item.UnitPrice : product.Price;

                orderItemsVerify.Add(orderItem);

                totalCalculatedAmount += orderItem.UnitPrice * orderItem.Quantity;
            }

            // Update order
            existingOrder.Items = orderItemsVerify;
            existingOrder.TotalAmount = totalCalculatedAmount;
            existingOrder.PaymentMethod = request.PaymentMethod;
            existingOrder.ReceiveInfo = request.ReceiveInfo;
            existingOrder.Status = "Pending"; // reset status
            existingOrder.CreatedAt = DateTime.UtcNow; // track update, preserve CreatedAt

            await _orderRepository.UpdateOrderAsync(existingOrder);

            return Ok(new { message = "Order updated successfully.", order = existingOrder });
        }







    }
}

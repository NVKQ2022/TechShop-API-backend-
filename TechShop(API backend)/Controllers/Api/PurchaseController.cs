using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Order;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;
using TechShop_API_backend_.Service;
using TechShop_API_backend_.Data.Authenticate;
namespace TechShop_API_backend_.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseController : ControllerBase
    {

        UserDetailRepository _detailRepository;
        UserRepository _userRepository;
        ProductRepository _productRepository;
        OrderRepository _orderRepository;
        PaymentService _vnPayService;
        JwtService _jwtService;
        private const string VALID_USERNAME = "techshop_username";
        private const string VALID_PASSWORD = "techshop_password";
        public PurchaseController(UserDetailRepository detailRepository,JwtService jwtService, UserRepository userRepository , ProductRepository productRepository, OrderRepository orderRepository,PaymentService vnpayService)
        {
            _detailRepository = detailRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
            _vnPayService = vnpayService;
            _jwtService = jwtService;
            _userRepository = userRepository;
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
            order.Status = "Pending";  // Change status to Confirmed
            order.CreatedAt = DateTime.UtcNow; // Add confirmation timestamp

            // 4. Save the updated order
            await _orderRepository.UpdateOrderAsync(order);

            // Optionally, you can trigger other actions such as notifying the user or sending a receipt

            return Ok(new { message = "Order confirmed successfully.", order });
        }


        //[HttpPost("create-qr")]
        //public async Task<IActionResult> CreateOrderQr([FromBody] string orderId)
        //{
        //    var order =await _orderRepository.GetOrderByIdAsync(orderId);

        //    string qrPaymentUrl = _vnPayService.CreateQrPaymentUrl(
        //        HttpContext,
        //        order.TotalAmount,
        //        order.OrderID
        //    );

        //    return Ok(new
        //    {
        //        orderId = order.OrderID,
        //        qrUrl = qrPaymentUrl
        //    });
        //}

        [AllowAnonymous]
        [HttpGet("payment-qr/{orderId}")]
        public async Task<IActionResult> GetPaymentQr(string orderId)
        {
            var order =  await _orderRepository.GetOrderByIdAsync(orderId);
            if(order == null || order.Status =="Received" )
            {
                return NotFound("no order found for this user or already payed" );
            }
            var amount = order.TotalAmount;

            string bankId = "BIDV";
            string account = "7011084307";
            string info = $"ORDER_{orderId}";

            string qrUrl = $"https://api.vietqr.io/image/{bankId}-{account}-qr_only.png?amount={amount}&addInfo={info}";
            string qrTestUrl = $"https://dev.vietqr.io/image/{bankId}-{account}-qr_only.png?amount={amount}&addInfo={info}";

            return Ok(new { qr= qrUrl,
                            amount = amount
                            , bankId = bankId
                            , account = account
            });
        }

        [AllowAnonymous]
        [HttpPost("vqr/api/token-generate")]
        public async Task<IActionResult> GenerateToken([FromHeader] string Authorization)
        {
            // Kiểm tra Authorization header
            if (string.IsNullOrEmpty(Authorization) || !Authorization.StartsWith("Basic "))
            {
                return BadRequest("Authorization header is missing or invalid");
            }

            // Giải mã Base64
            var base64Credentials = Authorization.Substring("Basic ".Length).Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(base64Credentials));
            var values = credentials.Split(':', 2);

            if (values.Length != 2)
            {
                return BadRequest("Invalid Authorization header format");
            }

            var username = values[0];
            var password = values[1];

            // Kiểm tra username và password
            if (username == VALID_USERNAME && password == VALID_PASSWORD)
            {
                var token = _jwtService.GenerateToken(await _userRepository.GetUserByUsernameAsync("vietqruser"));
                return Ok(new
                {
                    access_token = token,
                    token_type = "Bearer",
                    expires_in = 300 // Thời gian hết hạn token
                });
            }
            else
            {
                return Unauthorized("Invalid credentials");
            }
        }
    }
}

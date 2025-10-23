using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechShop_API_backend_.Data;
using TechShop_API_backend_.DTOs.Order;
using TechShop_API_backend_.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace TechShop_API_backend_.OrderControllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        OrderRepository orderRepository;
        public OrderController(OrderRepository orderRepository)
        {
            this.orderRepository = orderRepository;
        }




        // GET: api/<ValuesController>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var orders = await orderRepository.GetOrdersByUserAsync(int.Parse(userId!));

            return Ok(orders);
        }

        // GET api/<ValuesController>/5
        [Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetails(string id)
        {
            var order = await orderRepository.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return Ok(order);
        }

        // POST api/<ValuesController>
        [Authorize]
        [HttpPost]
        public void Post([FromBody] string value)
        {

        }

        // PUT api/<ValuesController>/5
        [Authorize (Roles="Admin")]
        [HttpPut("Admin/{orderId}")]
        
        public void OrderUpdate(string orderid, [FromBody] string value)
        {
        }




        // PUT api/<ValuesController>/5
        [Authorize]
        [HttpPut("{orderId}")]
        public async Task<IActionResult> UpdateOrder(string orderId, [FromBody] UpdateOrderRequest request)
        {
            var success = await orderRepository.UpdateOrderAsync(orderId, request);

            if (!success)
                return NotFound("Order not found or update failed.");

            return Ok("Order updated successfully.");
        }


        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using TechShop_API_backend_.DTOs.Order;
using TechShop_API_backend_.Interfaces;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Data
{
    public class OrderRepository
    {
        private readonly IMongoCollection<Order> _orders;

        public OrderRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _orders = database.GetCollection<Order>(settings.Value.OrderCollectionName);
        }


        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _orders.Find(_ => true).ToListAsync();
        }


        public async Task CreateOrderAsync(Order order)
        {
            await _orders.InsertOneAsync(order);
        }

        public async Task<Order> GetOrderByIdAsync(string id)
        {
            return await _orders.Find(o => o.OrderID == id).FirstOrDefaultAsync();
         
        }

        public async Task<List<Order>> GetOrdersByUserAsync(int userId)
        {
            return await _orders.Find(o => o.UserID == userId).ToListAsync();

        }

        public async Task<bool> CancelOrder(string orderId)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderID, orderId);
            var update = Builders<Order>.Update.Set(o => o.Status, "Cancelled");

            await _orders.UpdateOneAsync(filter, update);

            return true; // Replace with actual view name
        }


        public async Task<Order> ChooseProductToReview(string orderId)
        {
            var order = await _orders.Find(o => o.OrderID == orderId).FirstOrDefaultAsync();
            if (order == null || order.Status != "Delivered")
                return null;

            return order; // Show the list of items in that order
        }

        public async Task AddOrderAsync(Order order)
        {
            await _orders.InsertOneAsync(order);
            // decrease // stock of each product in the order
        }




        public async Task<bool> UpdateOrderAsync( Order updatedOrder)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderID, updatedOrder.OrderID);
            var update = Builders<Order>.Update
                .Set(o => o.Items, updatedOrder.Items)
                .Set(o => o.TotalAmount, updatedOrder.TotalAmount)
                .Set(o => o.PaymentMethod, updatedOrder.PaymentMethod)
                .Set(o => o.ReceiveInfo, updatedOrder.ReceiveInfo)
                .Set(o => o.Status, updatedOrder.Status);

            var result = await _orders.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }


        public async Task<bool> UpdateOrderStatusAsync(string orderId, string newStatus)
        {
            var filter = Builders<Order>.Filter.Eq(o => o.OrderID, orderId);
            var update = Builders<Order>.Update.Set(o => o.Status, newStatus);
            var result = await _orders.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteOrderAsync(string id)
        {
            var result = await _orders.DeleteOneAsync(o => o.OrderID == id);
            return result.IsAcknowledged && result.DeletedCount > 0;
        }

    }
}

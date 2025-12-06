using MongoDB.Driver;
using TechShop_API_backend_.Models;
using Microsoft.Extensions.Options;

namespace TechShop_API_backend_.Data
{
    public class NotificationRepository
    {
        private readonly IMongoCollection<Notification> _notification;

        public NotificationRepository(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.DatabaseName);
            _notification = database.GetCollection<Notification>(settings.Value.NotificationCollectionName);
        }

        // Create a new notification
        public async Task CreateAsync(Notification notification)
        {
            await _notification.InsertOneAsync(notification);
        }

        // Get unread notifications for a specific user
        public async Task<List<Notification>> GetUnreadAsync(string username)
        {
            return await _notification
                .Find(n => n.Username == username && !n.IsRead)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // Get all notifications for a specific user
        public async Task<List<Notification>> GetAllByUserAsync(string username)
        {
            return await _notification
                .Find(n => n.Username == username)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        // Mark a notification as read
        public async Task MarkAsReadAsync(string id)
        {
            var filter = Builders<Notification>.Filter.Eq(n => n.Id, id);
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            await _notification.UpdateOneAsync(filter, update);
        }

        // Optional: mark all notifications for a user as read
        public async Task MarkAllAsReadAsync(string username)
        {
            var filter = Builders<Notification>.Filter.Eq(n => n.Username, username);
            var update = Builders<Notification>.Update.Set(n => n.IsRead, true);
            await _notification.UpdateManyAsync(filter, update);
        }
    }
}

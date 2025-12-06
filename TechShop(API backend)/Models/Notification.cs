using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TechShop_API_backend_.Models
{
    public class Notification
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } =null!;

        public string Title { get; set; }

        // Dictionary for multiple key-value messages
        public Dictionary<string, string> Body { get; set; } = new Dictionary<string, string>();

        public string Username { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

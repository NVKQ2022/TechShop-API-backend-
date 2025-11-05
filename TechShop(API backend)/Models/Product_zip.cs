using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace TechShop_API_backend_.Models
{
    public class Product_zip
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Product_zipId { get; set; }

        public string Name { get; set; }

        public int QuantitySold { get; set; }

        public string Image { get; set; } //first img of the list

        public SaleInfo Sale { get; set; }

        public int Price { get; set; }

        public float Rating { get; set; } // Rating in stars (e.g., 4.5)
    }
}

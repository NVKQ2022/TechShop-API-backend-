using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ProductSaleEvent
{
  [BsonId]
  [BsonRepresentation(BsonType.ObjectId)]
  public string Id { get; set; }

  public string Title { get; set; }
  public string Color { get; set; } // "Danger" | "Success" | "Primary" | "Warning"

  [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
  public DateTime StartDate { get; set; }

  [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
  public DateTime EndDate { get; set; }

  public double Percent { get; set; } // 0–1

  public List<string> ProductIds { get; set; } = new();
}

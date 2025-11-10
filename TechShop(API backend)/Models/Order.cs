using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Reflection;
using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Models
{
    public class Order
    {

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string OrderID { get; set; }  // Maps to "_id"

        public int UserID { get; set; }

        public List<OrderItem> Items { get; set; }

        public int TotalAmount { get; set; }

        public string? PaymentMethod { get; set; }

        public string Status { get; set; } //Pending: The order has been received but not yet processed.

        //Processing: The order is being prepared or assembled.

        //Shipped: The order has been sent out to the customer.

        //Delivered: The customer has received the order.


        //Cancelled: The order was canceled before it was fulfilled.


                [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }
        public ReceiveInfo? ReceiveInfo { get; set; }

    }

    public class OrderItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string ProductID { get; set; }

        public string ProductName { get; set; }

        public string Image { get; set; }

        public int Quantity { get; set; }

        public int UnitPrice { get; set; }
    }

}

﻿namespace TechShop_API_backend_.Models
{
    
    public class CartItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Image { get; set; }
        
        public int UnitPrice { get; set; }
        public int Quantity { get; set; }
    }
}

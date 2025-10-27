﻿using TechShop_API_backend_.Models;

namespace TechShop_API_backend_.Helpers
{
    public class ConverterHelper
    {
        public Product_zip ConvertProductToProductZip(Product product)
        {

            int totalRatings = 0;
            int weightedSum = 0;
            foreach (var pair in product.Rating)
            {
                // Extract the numeric part from the key: "rate_1" -> 1
                if (int.TryParse(pair.Key.Replace("rate_", ""), out int starValue))
                {
                    int count = pair.Value;
                    weightedSum += starValue * count;
                    totalRatings += count;
                }
            }

            float averageRating = totalRatings == 0 ? 0 : (float)weightedSum / totalRatings;
            double roundedAverage = Math.Round(averageRating, 1);
            // Create a new Product_zip object
            var productZip = new Product_zip
            {
                // Mapping ProductId to Product_zipId
                Product_zipId = product.ProductId,

                // Mapping Name directly
                Name = product.Name,

                // Mapping Sold to QuantitySold
                QuantitySold = product.Sold,

                // Getting the first image from the list or setting to null if the list is empty
                Image = product.ImageURL?.FirstOrDefault(),

                // Mapping Price directly
                Price = product.Price,





                Rating = (float)roundedAverage

            };
               

        

            return productZip;
        }


        public List<Product_zip> ConvertProductListToProductZipList(List<Product> products)
        {
            // Check for null input
            if (products == null)
                return new List<Product_zip>();

            // Convert each Product to Product_zip using the existing method
            return products.Select(ConvertProductToProductZip).ToList();
        }

        public  CartItem ConvertProductToCartItem(Product product)
        {
            return new CartItem
            {


                Product= new Product_zip
                {
                    Product_zipId = product.ProductId,
                    Name = product.Name,
                    QuantitySold = product.Sold,
                    Image = product.ImageURL?.FirstOrDefault(),
                    Price = product.Price,
                    Rating = product.Rating.Count == 0 ? 0 : (float)product.Rating.Sum(r =>
                    {
                        var starValue = int.Parse(r.Key.Replace("rate_", ""));
                        return starValue * r.Value;
                    }) / product.Rating.Values.Sum()
                },
                
                Quantity = 1  // Defaulting to quantity of 1 (you can customize this)
            };
        }

    }
}

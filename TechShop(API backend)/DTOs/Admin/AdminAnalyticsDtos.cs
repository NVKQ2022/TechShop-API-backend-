using System;
using System.Collections.Generic;

namespace TechShop_API_backend_.DTOs.Admin
{
  // 1) Orders – daily stats
  public class AdminDailyOrderStatDto
  {
    public DateTime Date { get; set; }
    public int TotalOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal Revenue { get; set; } // chỉ Delivered
  }

  // 2) Orders – status distribution
  public class AdminOrderStatusDistributionDto
  {
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
  }

  // 3) Orders – payment methods
  public class AdminPaymentMethodStatDto
  {
    public string PaymentMethod { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
    public decimal Revenue { get; set; }
  }

  // 4) Products – category stats
  public class AdminProductCategoryStatDto
  {
    public string Category { get; set; } = string.Empty;
    public int ProductsCount { get; set; }
    public int TotalStock { get; set; }
    public int TotalSold { get; set; }
  }

  // 5) Products – rating distribution (toàn shop)
  public class AdminRatingDistributionDto
  {
    public int RatingValue { get; set; } // 1–5
    public int Count { get; set; }
  }

  // 6) Users – demographics
  public class AdminGenderDistributionDto
  {
    public string Gender { get; set; } = string.Empty;
    public int Count { get; set; }
  }

  public class AdminAgeBucketDto
  {
    public string Range { get; set; } = string.Empty; // "18–24"...
    public int Count { get; set; }
  }

  public class AdminDemographicsDto
  {
    public List<AdminGenderDistributionDto> Gender { get; set; } = new();
    public List<AdminAgeBucketDto> Age { get; set; } = new();
  }

  // 7) Sale events – summary
  public class AdminSaleEventsSummaryDto
  {
    public int TotalEvents { get; set; }
    public int ActiveEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int EndedEvents { get; set; }
  }
}

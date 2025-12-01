using System.ComponentModel.DataAnnotations;
using TechShop_API_backend_.Models;
namespace TechShop_API_backend_.DTOs.Admin
{
  public class CreateProductDto
  {
    [Required]
    public string Name { get; set; }

    public string? Description { get; set; }

    [Range(0, int.MaxValue)]
    public int Price { get; set; }

    public List<string>? Color { get; set; }

    public List<string>? ImageURL { get; set; }

    public Dictionary<string, string>? Detail { get; set; }

    [Required]
    public string Category { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }
    public SaleInfo? Sale { get; set; }
  }
}
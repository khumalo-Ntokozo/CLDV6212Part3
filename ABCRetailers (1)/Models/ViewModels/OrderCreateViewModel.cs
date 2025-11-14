using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string CustomerId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        // Navigation properties for dropdowns
        public List<Customer> Customers { get; set; } = new();
        public List<Product> Products { get; set; } = new();

        // Price properties as double to match Product
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }
}

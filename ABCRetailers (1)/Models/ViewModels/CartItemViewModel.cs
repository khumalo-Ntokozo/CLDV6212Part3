// Update your CartViewModel classes to match what the view expects
namespace ABCRetailers.ViewModels
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public double Price { get; set; } // Changed from decimal to double
        public int Quantity { get; set; }
        public double TotalPrice => Price * Quantity; // Changed from decimal to double
        public string ImageUrl { get; set; }
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new();
        public double TotalAmount { get; set; } // Changed from decimal to double
    }
}
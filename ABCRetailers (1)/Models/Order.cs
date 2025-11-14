using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [NotMapped]
        public string ETagString
        {
            get => ETag.ToString();
            set => ETag = string.IsNullOrEmpty(value) ? ETag.All : new ETag(value);
        }

        [Display(Name = "Order ID")]
        public string OrderId => RowKey;

        [Display(Name = "Customer ID")]
        public string CustomerId { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Product ID")]
        public string ProductId { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price")]
        public double UnitPrice { get; set; } // Changed from decimal to double

        [Display(Name = "Total Price")]
        public double TotalPrice { get; set; } // Changed from decimal to double

        [Display(Name = "Status")]
        public string Status { get; set; } = "PENDING";
    }
}
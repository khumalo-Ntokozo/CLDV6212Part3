using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [NotMapped]
        public string ETagString
        {
            get => ETag.ToString();
            set => ETag = string.IsNullOrEmpty(value) ? ETag.All : new ETag(value);
        }

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public double Price { get; set; }  // Make sure this is double

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
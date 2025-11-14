using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerUsername { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.Now;
    }
}
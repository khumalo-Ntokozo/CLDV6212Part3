using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(256)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Customer" or "Admin"

        // Remove any Azure Table specific properties like ETag, PartitionKey, RowKey, Timestamp
        // These are only for Azure Table Storage entities, not for SQL Server entities
    }
}
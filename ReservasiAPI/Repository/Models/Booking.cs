using ReservasiAPI.Repository.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservasiAPI.Repository.Models
{
    [Table("booking")]
    public class Booking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string? Fullname { get; set; }
        public string? Email { get; set; }

        [Required]
        public DateTime CheckinDate { get; set; }

        [Required]
        public DateTime CheckoutDate { get; set; }

        [Required]
        public required string RoomType { get; set; }

        public int AdultGuests { get; set; }
        public int ChildGuests { get; set; }

        public string? SpecialRequest { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public required string PhoneNumber { get; set; }

        [Required]
        public required string PaymentMethod { get; set; }

        [Column("paymentstatus")]
        [Required]
        public string PaymentStatus { get; set; } = "Pending (Pay On Arrive)";

        [Column("roomid")]
        public int? RoomId { get; set; }

        [ForeignKey("RoomId")]
        public Room? Room { get; set; }

        [Column("midtransorderid")]
        public string? MidtransOrderId { get; set; }
    }
}
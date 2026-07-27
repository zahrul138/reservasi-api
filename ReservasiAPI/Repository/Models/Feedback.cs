using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservasiAPI.Repository.Models;

[Table("feedbacks")]
public partial class Feedback
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [Column("email")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("subject")]
    [StringLength(150)]
    public string? Subject { get; set; }

    [Column("inquirytype")]
    [StringLength(50)]
    public string? InquiryType { get; set; }

    [Column("message")]
    [StringLength(2000)]
    public string? Message { get; set; }

    [Column("status")]
    [StringLength(20)]
    public string? Status { get; set; }

    [Column("createdat", TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }
}
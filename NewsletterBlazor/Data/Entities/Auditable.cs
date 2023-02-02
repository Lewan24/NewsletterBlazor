using System.ComponentModel.DataAnnotations;

namespace NewsletterBlazor.Data.Entities;

public class Auditable
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
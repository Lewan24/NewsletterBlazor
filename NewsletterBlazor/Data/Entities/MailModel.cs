using System.ComponentModel.DataAnnotations;

namespace NewsletterBlazor.Data.Entities;

public record MailModel
{
    [Required]
    [StringLength(50)]
    public string Subject { get; set; } = "";
    [Required]
    [StringLength(600)]
    public string Body { get; set; } = "";
    public Stream? Attachtment { get; set; }
    [Required]
    public List<string> Receivers { get; set; }
    public bool IsHTML { get; set; } = false;
}

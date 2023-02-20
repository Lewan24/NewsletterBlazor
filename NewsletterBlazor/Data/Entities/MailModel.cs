using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;

namespace NewsletterBlazor.Data.Entities;

public record MailModel
{
    [Required]
    [StringLength(50)]
    public string Subject { get; set; } = "";
    [Required]
    [StringLength(600)]
    public string Body { get; set; } = "";

    public List<IBrowserFile> Attachtments { get; set; } = new();
    public List<(string FilePath, string FileName, string FileContentType)> TempAttachFiles { get; set; } = new();
    public bool IsHTML { get; set; } = false;
}

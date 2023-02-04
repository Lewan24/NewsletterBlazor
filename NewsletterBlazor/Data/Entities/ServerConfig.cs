using System.ComponentModel.DataAnnotations;

namespace NewsletterBlazor.Data.Entities;

#nullable disable warnings

public record ServerConfig
{
    public string Server { get; set; }
    public int Port { get; set; } = 587;
    [EmailAddress]
    public string EmailSender { get; set; }
    public string EmailName { get; set; }
    public string EmailPassword { get; set; }
}
namespace NewsletterBlazor.Data.Entities;

#nullable disable warnings

public record ServerConfig
{
    public string Server { get; set; }
    public int Port { get; set; } = 587;
    public string EmailSender { get; set; }
    public string EmailPassword { get; set; }
}

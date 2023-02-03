using NewsletterBlazor.Data.Entities;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;

    protected override void OnInitialized()
    {
        _serverConfig = new()
        {
            Server = _configuration["ServerConfig:Server"],
            Port = Int32.Parse(_configuration["ServerConfig:Port"]),
            EmailSender = _configuration["ServerConfig:Email"],
            EmailPassword = _configuration["ServerConfig:Password"]
        };
    }
}
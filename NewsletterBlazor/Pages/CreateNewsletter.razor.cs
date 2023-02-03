using NewsletterBlazor.Data.Entities;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private MailModel _mailModel = new();
    private Stream tempReceiversListAsFile;

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

    private void SendNewsletter()
    {
        _logger.LogInformation("Submit button clicked");
    }
}
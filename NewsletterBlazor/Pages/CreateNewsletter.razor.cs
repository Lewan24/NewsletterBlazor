using NewsletterBlazor.Data.Entities;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private MailModel _mailModel = new();
    private Stream tempReceiversListAsFile;
    private State _state = new();

    private class State
    {
        public string Success { get; set; } = "";
        public string Warning { get; set; } = "";
        public string Error { get; set; } = "";

        public void Clear()
        {
            Success = ""; Warning = ""; Error = "";
        }
    }

    protected override void OnInitialized()
    {
        _state.Clear();

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
        _mailModel.Receivers.Add("asd");

        if (_mailModel.Receivers.Count == 0)
        {
            _state.Error = "There are no Receivers";
            return;
        }

        _logger.LogInformation("Submit button clicked");
        _state.Success = "Successfully sent emails.";

        _mailModel = new();
    }
}
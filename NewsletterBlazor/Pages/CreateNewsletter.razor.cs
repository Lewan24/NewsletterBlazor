using Microsoft.AspNetCore.Components.Forms;
using NewsletterBlazor.Data.Entities;
using System.IO;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private MailModel _mailModel = new();
    private IBrowserFile tempReceiversListAsFile;
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

    private async Task LoadReceivers(InputFileChangeEventArgs e)
    {
        _mailModel.Receivers.Clear();

        try
        {
            var file = e.File;
            using (var streamReader = new StreamReader(file.OpenReadStream()))
            {
                var content = await streamReader.ReadToEndAsync();
                _mailModel.Receivers = content.Split("\n").ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Error: {Error}", ex.Message);
        }
    }

    private async Task HandleForm()
    {
        _state.Clear();

        if (_mailModel.Receivers.Count == 0)
        {
            _state.Error = "There are no Receivers";
            return;
        }

        _logger.LogInformation($"Successfully read file, number of receivers: {_mailModel.Receivers.Count}");

        _mailModel = new();
    }
}
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Data.SqlClient;
using NewsletterBlazor.Data.Entities;
using System.IO;
using System.Net;
using System.Net.Mail;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private MailModel _mailModel = new();
    private IBrowserFile tempReceiversListAsFile;
    private State _state = new();
    private const int MaxAttemptsForSend = 3;
    private List<string> BadReceivers = new();
    private bool IsSending = false;
    private int ActualEmail = 1;
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
            EmailName = _configuration["ServerConfig:EmailName"],
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

        _logger.LogInformation("Creating Message");

        var mailmessage = new MailMessage()
        {
            Subject = _mailModel.Subject,
            Body = _mailModel.Body,
            IsBodyHtml = _mailModel.IsHTML,
            From = new MailAddress(_serverConfig.EmailSender, _serverConfig.EmailName),
            Sender = new MailAddress(_serverConfig.EmailSender),
        };

        mailmessage.ReplyToList.Add(_AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name);

        _logger.LogInformation("Adding message to history");

        await _context.HistoryOfUses.AddAsync(new HistoryOfUses()
        {
            CreatedBy = _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name,
            UserId = Guid.Parse(_context.Users.FirstOrDefault(u => u.UserName == _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name).Id),
            HowManyEmailsSent = _mailModel.Receivers.Count,
            SubjectOfEmail = mailmessage.Subject,
            BodyOfEmail = mailmessage.Body
        });

        _logger.LogInformation("Saving changes");

        await _context.SaveChangesAsync();


        _logger.LogInformation("Creating smtp client...");

        SmtpClient client = new(_serverConfig.Server, _serverConfig.Port)
        {
            Credentials = new NetworkCredential(_serverConfig.EmailSender, _serverConfig.EmailPassword)
        };

        IsSending = true;

        foreach (var receiver in _mailModel.Receivers)
        {
            mailmessage.To.Clear();
            
            // TODO: Throws error while adding string/receiver to list
            mailmessage.To.Add(receiver);

            _logger.LogInformation($"Sending email to: {receiver}, {ActualEmail}/{_mailModel.Receivers.Count}");

            ActualEmail++;

            int attempt = 1;

            while (attempt <= MaxAttemptsForSend)
            {
                if (await TrySendMailAsync(mailmessage, client))
                {
                    attempt = 1;
                    break;
                }

                attempt++;
            }
        }

        IsSending = false;
        _state.Success = $"Successfully sent {_mailModel.Receivers.Count} emails";
        _mailModel = new();
    }

    private async Task<bool> TrySendMailAsync(MailMessage mail, SmtpClient client)
    {
        try
        {
            //await client.SendMailAsync(mail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error while sending email: {ex.Message}");
            BadReceivers.Add(mail.To.ToString());

            return false;
        }
    }
}
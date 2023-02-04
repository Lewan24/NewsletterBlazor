using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity.UI.Services;
using NewsletterBlazor.Data.Entities;
using NuGet.Protocol.Plugins;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private State _state = new();

    private MailModel _mailModel = new();
    private IBrowserFile tempReceiversListAsFile;
    private List<string> ReceiversList = new();
    private List<string> BadReceivers = new();

    private int ActualEmail = 0;
    private int SuccessfullySent = 0;
    private const int MaxAttemptsForSend = 3;
    private bool IsSending = false;

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
        ReceiversList.Clear();

        try
        {
            var file = e.File;
            using (var streamReader = new StreamReader(file.OpenReadStream()))
            {
                var content = await streamReader.ReadToEndAsync();
                ReceiversList = content.Split("\r\n").ToList();

                _logger.LogInformation("Loaded mails");
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

        if (ReceiversList.Count == 0)
        {
            _state.Error = "There are no Receivers";
            return;
        }

        _logger.LogInformation($"Successfully read file, number of receivers: {ReceiversList.Count}");

        _logger.LogInformation("Adding message to history");

        await _context.HistoryOfUses.AddAsync(new HistoryOfUses()
        {
            CreatedBy = _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name,
            UserId = Guid.Parse(_context.Users.FirstOrDefault(u => u.UserName == _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name).Id),
            HowManyEmailsSent = ReceiversList.Count,
            SubjectOfEmail = _mailModel.Subject,
            BodyOfEmail = _mailModel.Body
        });

        _logger.LogInformation("Saving changes");

        await _context.SaveChangesAsync();

        _logger.LogInformation("Creating smtp client...");

        SmtpClient client = new(_serverConfig.Server, _serverConfig.Port)
        {
            Credentials = new NetworkCredential(_serverConfig.EmailSender, _serverConfig.EmailPassword),
            EnableSsl = true,
            DeliveryFormat = SmtpDeliveryFormat.International
        };

        ActualEmail = 0;
        SuccessfullySent = 0;

        int attempt = 1;

        IsSending = true;
        await InvokeAsync(StateHasChanged);

        foreach (var receiver in ReceiversList)
        {
            attempt = 1;

            ActualEmail++;
            await InvokeAsync(StateHasChanged);

            while (attempt <= MaxAttemptsForSend)
            {
                if (await TrySendMailAsync(receiver, client))
                {
                    attempt = 1;
                    SuccessfullySent++;
                    break;
                }

                attempt++;
            }

            if (attempt > MaxAttemptsForSend)
                BadReceivers.Add(receiver);
        }

        _state.Success = $"Successfully sent {SuccessfullySent}/{ReceiversList.Count} emails";
        IsSending = false;
        await InvokeAsync(StateHasChanged);

        ReceiversList.Clear();
        _mailModel = new();
    }

    private async Task<bool> TrySendMailAsync(string receiver, SmtpClient client)
    {
        var currentUser = _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name;

        try
        {
            _logger.LogInformation("Creating Message");

            using (var mail = new MailMessage(_serverConfig.EmailSender, receiver))
            {
                mail.Subject = _mailModel.Subject;
                mail.Body = _mailModel.Body;
                mail.IsBodyHtml = _mailModel.IsHTML;
                mail.From = new MailAddress(_serverConfig.EmailSender, _serverConfig.EmailName);
                mail.Sender = new MailAddress(_serverConfig.EmailSender);

                mail.ReplyToList.Add(currentUser);

                _logger.LogInformation($"Sending to: {receiver}");

                await client.SendMailAsync(mail);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error while sending email: {ex.Message}");

            return false;
        }
    }
}
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using NewsletterBlazor.Data.Entities;
using System.Net;
using System.Net.Mail;
using NuGet.Packaging;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

public record State
{
    public string Success { get; set; } = "";
    public string Warning { get; set; } = "";
    public string Error { get; set; } = "";

    public void Clear()
    {
        Success = ""; Warning = ""; Error = "";
    }
}

partial class CreateNewsletter
{
    private ServerConfig _serverConfig;
    private State _applicationState = new();
    private State _attachmentsState = new();

    private MailModel _mailModel = new();
    
    private List<string> _receiversList = new();
    private List<string> _badReceivers = new();

    private int ActualEmail = 0;
    private int SuccessfullySent = 0;
    private const int MaxAttemptsForSend = 3;
    private bool IsSending = false;
    private bool IsLoadingFiles = false;

    protected override void OnInitialized()
    {
        _applicationState.Clear();

        _serverConfig = new()
        {
            Server = _configuration["ServerConfig:Server"],
            Port = Int32.Parse(_configuration["ServerConfig:Port"]),
            EmailSender = _configuration["ServerConfig:Email"],
            EmailName = _configuration["ServerConfig:EmailName"],
            EmailPassword = _configuration["ServerConfig:Password"]
        };
    }

    private async Task AddAttachtment(InputFileChangeEventArgs args)
    {
        _attachmentsState.Clear();

        if (_mailModel.Attachtments.Count >= 5)
        {
            _attachmentsState.Warning = "There can be only 5 attachments in mail";
            StateHasChanged();
            return;
        }

        _logger.LogWarning($"Adding attachment: {args.File}\nInfo: {args.File.Name}, {args.File.Size}");
        _mailModel.Attachtments.Add(args.File);
        
        try
        {
            _logger.LogInformation("Creating copy of uploaded file...");
            var path = Path.Combine(Path.GetTempPath(), args.File.Name);
            _logger.LogWarning($"IBrowserFile: {args.File.Name}, {args.File.ContentType}, {args.File.Size}\nTemp path: {path}");

            await using var fs = new FileStream(path, FileMode.Create);

            _logger.LogInformation("Reading and copying item...");
            await args.File.OpenReadStream().CopyToAsync(fs);
            _mailModel.TempAttachFiles.Add(new(path, args.File.Name, args.File.ContentType, args.File.Size));
        }
        catch (Exception e)
        {
            _logger.LogError($"Problem with file: {e.Message}, Base ex: {e.GetBaseException().Message}");
        }
    }

    private void RemoveAttachtment(string filename, long size)
    {
        _attachmentsState.Clear();
        File.Delete(_mailModel.TempAttachFiles.FirstOrDefault(f => f.FileName == filename && f.FileSize == size).FilePath);
        _mailModel.Attachtments.Remove(_mailModel.Attachtments.FirstOrDefault(f => f.Name == filename && f.Size == size));
        _mailModel.TempAttachFiles.Remove(_mailModel.TempAttachFiles.FirstOrDefault(f => f.FileName == filename && f.FileSize == size));
    }

    private async Task LoadReceivers(InputFileChangeEventArgs args)
    {
        _receiversList.Clear();

        try
        {
            using (var streamReader = new StreamReader(args.File.OpenReadStream()))
            {
                var content = await streamReader.ReadToEndAsync();
                _receiversList = content.Split("\r\n").ToList();

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
        _badReceivers.Clear();
        _applicationState.Clear();
        _attachmentsState.Clear();

        if (_receiversList.Count == 0)
        {
            _applicationState.Error = "There are no Receivers";
            return;
        }
        
        StateHasChanged();

        _logger.LogInformation($"Successfully read file, number of receivers: {_receiversList.Count}");

        _logger.LogInformation("Adding message to history");

        await _context.HistoryOfUses.AddAsync(new HistoryOfUses()
        {
            CreatedBy = _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name,
            UserId = Guid.Parse(_context.Users.FirstOrDefault(u => u.UserName == _AuthenticationStateProvider.GetAuthenticationStateAsync().Result.User.Identity.Name).Id),
            HowManyEmailsSent = _receiversList.Count,
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

        foreach (var receiver in _receiversList)
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
                _badReceivers.Add(receiver);
        }

        await Task.Run(DeleteTempFiles);

        _applicationState.Success = $"Successfully sent {SuccessfullySent}/{_receiversList.Count} emails";
        IsSending = false;
        await InvokeAsync(StateHasChanged);

        _receiversList.Clear();

        if (_badReceivers.Count == 0)
            _mailModel = new();
    }

    private void DeleteTempFiles()
    {
        foreach (var tempFile in _mailModel.TempAttachFiles)
        {
            _logger.LogInformation($"Deleting: {tempFile.FileName}, {tempFile.FilePath}");
            File.Delete(tempFile.FilePath);
        }
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

                foreach (var file in _mailModel.TempAttachFiles)
                {
                    var attachmentData = new Attachment(file.FilePath);
                    mail.Attachments.Add(attachmentData);
                }

                _logger.LogInformation($"Sending to: {receiver}");

                await client.SendMailAsync(mail);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error while sending email: {ex.Message}, Base Message: {ex.GetBaseException().Message}");

            return false;
        }
    }

    private async Task SendMailAgainToBadEmails()
    {
        _receiversList.AddRange(_badReceivers);
        StateHasChanged();
        _badReceivers.Clear();

        _applicationState.Warning = "In 1 sec system will try to send emails again...";
        await Task.Delay(1000);

        await HandleForm();
    }
}
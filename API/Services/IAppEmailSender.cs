namespace API.Services;

public interface IAppEmailSender
{
    bool IsConfigured { get; }
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken);
}

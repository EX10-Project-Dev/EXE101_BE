using System.Net;
using System.Net.Mail;

namespace API.Services;

public sealed class SmtpAppEmailSender(IConfiguration configuration, ILogger<SmtpAppEmailSender> logger) : IAppEmailSender
{
    private IConfigurationSection Settings => configuration.GetSection("Email");

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Settings["Host"]) &&
        !string.IsNullOrWhiteSpace(Settings["From"]);

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("SMTP email settings are not configured.");

        var host = Settings["Host"]!;
        var port = int.TryParse(Settings["Port"], out var parsedPort) ? parsedPort : 587;
        var from = Settings["From"]!;
        var username = Settings["Username"];
        var password = Settings["Password"];
        var enableSsl = !bool.TryParse(Settings["EnableSsl"], out var parsedSsl) || parsedSsl;

        using var message = new MailMessage(from, recipient, subject, htmlBody) { IsBodyHtml = true };
        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(username))
            client.Credentials = new NetworkCredential(username, password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send an account email.");
            throw;
        }
    }
}

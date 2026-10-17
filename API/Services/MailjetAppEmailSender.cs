using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Text;

namespace API.Services;

public sealed class MailjetAppEmailSender(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<MailjetAppEmailSender> logger) : IAppEmailSender
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(configuration["Email:ApiKey"]) &&
        !string.IsNullOrWhiteSpace(configuration["Email:SecretKey"]) &&
        !string.IsNullOrWhiteSpace(configuration["Email:From"]);

    public async Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Configure Email:ApiKey, Email:SecretKey and Email:From for Mailjet.");

        var apiKey = configuration["Email:ApiKey"]!.Trim();
        var secretKey = configuration["Email:SecretKey"]!.Trim();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.mailjet.com/v3.1/send");
        var basicToken = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{secretKey}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        var from = new MailAddress(configuration["Email:From"]!.Trim());
        request.Content = JsonContent.Create(new
        {
            Messages = new[]
            {
                new
                {
                    From = new { Email = from.Address, Name = string.IsNullOrWhiteSpace(from.DisplayName) ? "FIND&GO" : from.DisplayName },
                    To = new[] { new { Email = recipient } },
                    Subject = subject,
                    HTMLPart = htmlBody
                }
            }
        });

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Mailjet rejected an account email with HTTP {StatusCode}.", (int)response.StatusCode);
                throw new HttpRequestException("Email provider rejected the request.", null, response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Mailjet account email request timed out.");
            throw;
        }
        catch (HttpRequestException exception)
        {
            logger.LogError("Mailjet account email request failed (HTTP {StatusCode}).", exception.StatusCode);
            throw;
        }
    }
}

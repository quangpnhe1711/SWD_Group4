using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SWD_Group4.BusinessLogic.IServices;
using SWD_Group4.DataAccess.Context;
using System.Net;
using System.Net.Mail;

namespace SWD_Group4.BusinessLogic.Services;

public sealed class NotificationService : INotificationService
{
    private readonly BookStoreContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(BookStoreContext dbContext, IConfiguration configuration, ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task sendVerifyNotification(int userId, bool isSuccessful, string? reason)
    {
        var userEmail = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            _logger.LogWarning("Unable to send email: userId={UserId} has no email.", userId);
            return;
        }

        var subject = isSuccessful
            ? "Verification approved"
            : "Verification rejected";

        var body = isSuccessful
            ? "Your verification request has been approved. Your verified information has been updated."
            : $"Your verification request has been rejected. Reason: {reason}";

        await SendEmailAsync(userEmail.Trim(), subject, body);
    }

    public async Task notifySellerSuspended(int userId, string reason, DateTime? suspensionEndAt)
    {
        var userEmail = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            _logger.LogWarning("Unable to send suspension email: userId={UserId} has no email.", userId);
            return;
        }

        var subject = "Account suspended";
        var untilText = suspensionEndAt == null
            ? "permanently"
            : $"until {suspensionEndAt:yyyy-MM-dd HH:mm}";

        var body = $"Your seller account has been suspended {untilText}. Reason: {reason}";
        await SendEmailAsync(userEmail.Trim(), subject, body);
    }

    public async Task notifySellerUnsuspended(int userId)
    {
        var userEmail = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(userEmail))
        {
            _logger.LogWarning("Unable to send unsuspension email: userId={UserId} has no email.", userId);
            return;
        }

        await SendEmailAsync(userEmail.Trim(), "Account reactivated", "Your seller account has been reactivated.");
    }

    private async Task SendEmailAsync(string to, string subject, string body)
    {
        // Read SMTP config in this priority order:
        // 1) Environment variables (easy per-developer override)
        // 2) appsettings.json (shared non-secret defaults)
        //
        // IMPORTANT: do NOT commit real passwords. Set SWD_SMTP_PASS (or SwdSmtp:Pass locally) per developer.
        var host = Environment.GetEnvironmentVariable("SWD_SMTP_HOST")
                   ?? _configuration["SwdSmtp:Host"]
                   ?? string.Empty;

        var portRaw = Environment.GetEnvironmentVariable("SWD_SMTP_PORT")
                      ?? _configuration["SwdSmtp:Port"];

        var user = Environment.GetEnvironmentVariable("SWD_SMTP_USER")
                   ?? _configuration["SwdSmtp:User"]
                   ?? string.Empty;

        var pass = Environment.GetEnvironmentVariable("SWD_SMTP_PASS")
                   ?? _configuration["SwdSmtp:Pass"]
                   ?? string.Empty;

        var from = Environment.GetEnvironmentVariable("SWD_SMTP_FROM")
                   ?? _configuration["SwdSmtp:From"]
                   ?? user;

        var sslRaw = Environment.GetEnvironmentVariable("SWD_SMTP_SSL")
                     ?? _configuration["SwdSmtp:EnableSsl"];

        // If not configured, fallback to mock.
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            _logger.LogInformation("[EMAIL-MOCK] To={To} | Subject={Subject} | Body={Body}", to, subject, body);
            return;
        }

        var port = 587;
        if (int.TryParse(portRaw, out var parsedPort) && parsedPort > 0)
        {
            port = parsedPort;
        }

        var enableSsl = true;
        if (bool.TryParse(sslRaw, out var parsedSsl))
        {
            enableSsl = parsedSsl;
        }

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(user, pass)
        };

        using var message = new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = false
        };

        try
        {
            await smtp.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed. Falling back to mock log. To={To} Subject={Subject}", to, subject);
            _logger.LogInformation("[EMAIL-MOCK-FALLBACK] To={To} | Subject={Subject} | Body={Body}", to, subject, body);
        }
    }
}

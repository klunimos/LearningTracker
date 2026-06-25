using System.Net;
using System.Net.Mail;

namespace LearningTracker.Api.Logic.Services;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}

/// <summary>
/// Sends mail through a configured SMTP server. When SMTP is not configured
/// (no Smtp:Host), it logs the message instead of sending — useful for local
/// development where you can copy the reset link straight from the logs.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration configuration;
    private readonly ILogger<SmtpEmailSender> logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        const string subject = "איפוס סיסמה - חלקנו";
        var body = BuildResetBody(resetLink);

        var host = configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning(
                "SMTP is not configured (Smtp:Host is empty). Password reset email for {Email} was NOT sent. Reset link: {ResetLink}",
                toEmail, resetLink);
            return;
        }

        var port = int.TryParse(configuration["Smtp:Port"], out var p) ? p : 587;
        var user = configuration["Smtp:User"];
        var password = configuration["Smtp:Password"];
        var fromAddress = configuration["Smtp:From"] ?? user ?? "no-reply@chelkenu.org";
        var fromName = configuration["Smtp:FromName"] ?? "חלקנו";
        var enableSsl = !bool.TryParse(configuration["Smtp:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
            BodyEncoding = System.Text.Encoding.UTF8,
            SubjectEncoding = System.Text.Encoding.UTF8
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(user))
            client.Credentials = new NetworkCredential(user, password);

        await client.SendMailAsync(message);
        logger.LogInformation("Password reset email sent to {Email}", toEmail);
    }

    private static string BuildResetBody(string resetLink)
    {
        return $@"<div dir=""rtl"" style=""font-family: Arial, sans-serif; font-size: 15px; color: #233f43;"">
  <p>שלום,</p>
  <p>קיבלנו בקשה לאיפוס הסיסמה לחשבונך ב<strong>חלקנו</strong>.</p>
  <p>לחץ על הקישור הבא כדי לבחור סיסמה חדשה (הקישור תקף לשעה אחת):</p>
  <p style=""margin: 24px 0;"">
    <a href=""{resetLink}"" style=""background:#488b91;color:#fff;padding:12px 24px;border-radius:10px;text-decoration:none;font-weight:bold;"">איפוס סיסמה</a>
  </p>
  <p>אם הכפתור אינו עובד, העתק את הכתובת הבאה לדפדפן:<br/>
    <span style=""word-break:break-all;color:#3f7b80;"">{resetLink}</span>
  </p>
  <p style=""color:#8EA0AA;font-size:13px;"">אם לא ביקשת לאפס את הסיסמה, ניתן להתעלם מהודעה זו.</p>
</div>";
    }
}

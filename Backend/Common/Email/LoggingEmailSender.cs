namespace Backend.Common.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendInvitationAsync(string toEmail, string fullname, string confirmationLink)
    {
        logger.LogInformation(
            "[EMAIL SIMULATION]: Invitation for {Fullname} <{Email}>\n Link: {Link}",
            fullname, toEmail, confirmationLink);
        return Task.CompletedTask;
    }
}
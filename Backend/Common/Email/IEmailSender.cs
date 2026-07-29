namespace Backend.Common.Email;

public interface IEmailSender
{
    Task SendInvitationAsync(string toEmail, string fullname, string confirmationLink);
}
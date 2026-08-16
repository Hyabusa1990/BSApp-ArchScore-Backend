using Azure;
using Azure.Communication.Email;
using System.Net.Mail;

namespace Fawkes.Api.Services
{
    public class EmailService
    {

        public EmailService() { }

        public async Task SendEmailAsync(string to, string subject, string body)
        {

            //string connectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING");
            var connectionString = @"endpoint=https://bulibowcommunicationservice.germany.communication.azure.com/;accesskey=REDACTED";
            var emailClient = new EmailClient(connectionString);


            var emailMessage = new EmailMessage(
                senderAddress: "DoNotReply@mail.bogenligen-hessen.org",
                content: new EmailContent(subject)
                {
                    PlainText = body
                },
                recipients: new EmailRecipients(new List<EmailAddress>
                {
                    new EmailAddress(to)
                }));


            EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                WaitUntil.Completed,
                emailMessage);

        }
    }
}

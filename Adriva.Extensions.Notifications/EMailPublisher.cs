using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adriva.Extensions.Notifications
{
    public class EMailPublisher : INotificationPublisher, IDisposable
    {
        protected readonly EMailPublisherOptions Options;
        private readonly ILogger Logger;
        private bool IsDisposed;

        protected SmtpClient SmtpClient { get; private set; }

        protected EMailPublisher(EMailPublisherOptions options, ILogger logger)
        {
            this.Options = options;
            this.Logger = logger;
        }

        public EMailPublisher(IOptions<EMailPublisherOptions> optionsAccessor, ILogger<EMailPublisher> logger) : this(optionsAccessor.Value, logger)
        {
            this.SmtpClient = new SmtpClient();
        }

        public Task InitializeAsync()
        {
            try
            {
                this.ConfigureSmtpClient();
            }
            catch (Exception innerException)
            {
                throw new InvalidOperationException("Error configuring Smtp client.", innerException);
            }

            return Task.CompletedTask;
        }

        protected virtual void ConfigureSmtpClient()
        {
            this.SmtpClient.Host = this.Options.Host;
            this.SmtpClient.Port = this.Options.Port;
            this.SmtpClient.UseDefaultCredentials = true;

            if (!string.IsNullOrWhiteSpace(this.Options.Username) && !string.IsNullOrWhiteSpace(this.Options.Password))
            {
                this.SmtpClient.UseDefaultCredentials = false;
                this.SmtpClient.Credentials = new NetworkCredential(this.Options.Username, this.Options.Password);
            }

            this.SmtpClient.EnableSsl = this.Options.UseSsl;
        }

        public bool CanPublish(NotificationMessage message)
        {
            return NotificationTarget.EMail == (message.Target & NotificationTarget.EMail);
        }

        protected virtual IEnumerable<Recipient> ResolveRecipients(NotificationMessage message, EMailRecipientTag tag)
        {
            return message.Recipients.Where(r =>
            {
                return (r.Tag is EMailRecipientTag eMailRecipientTag)
                && eMailRecipientTag == tag;
            });
        }

        protected virtual string ResolveContent(NotificationMessage message)
        {
            return Convert.ToString(message.Data) ?? string.Empty;
        }

        protected virtual string ResolveSubject(NotificationMessage message)
        {
            return this.Options.DefaultSubject ?? string.Empty;
        }

        public async Task PublishAsync(NotificationPublishContext context, CancellationToken cancellationToken)
        {
            using (MailMessage mailMessage = new MailMessage())
            {
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = this.ResolveSubject(context.Message);
                mailMessage.From = new MailAddress(this.Options.From);
                mailMessage.BodyEncoding = mailMessage.SubjectEncoding = Encoding.UTF8;

                foreach (var recipient in this.ResolveRecipients(context.Message, EMailRecipientTag.To))
                {
                    var mailAddress = new MailAddress(recipient.Address, recipient.FullName, Encoding.UTF8);
                    mailMessage.To.Add(mailAddress);
                }

                foreach (var recipient in this.ResolveRecipients(context.Message, EMailRecipientTag.Cc))
                {
                    var mailAddress = new MailAddress(recipient.Address, recipient.FullName, Encoding.UTF8);
                    mailMessage.CC.Add(mailAddress);
                }

                foreach (var recipient in this.ResolveRecipients(context.Message, EMailRecipientTag.Bcc))
                {
                    var mailAddress = new MailAddress(recipient.Address, recipient.FullName, Encoding.UTF8);
                    mailMessage.Bcc.Add(mailAddress);
                }
                try
                {
                    await this.SmtpClient.SendMailAsync(mailMessage);
                }
                catch (SmtpFailedRecipientException smtpFailedRecipientException)
                {
                    this.Logger.LogError(smtpFailedRecipientException, $"Error publishing e-mail notification to recipient '{smtpFailedRecipientException.FailedRecipient}'.");
                    throw;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.IsDisposed)
            {
                if (disposing)
                {
                    this.SmtpClient?.Dispose();
                }

                this.IsDisposed = true;
                this.SmtpClient = null;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

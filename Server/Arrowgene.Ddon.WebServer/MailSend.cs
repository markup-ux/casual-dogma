using Arrowgene.Ddon.Database.Model;
using Arrowgene.Logging;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Arrowgene.Ddon.WebServer
{
    public class MailSend
    {
        private static readonly ILogger Logger = LogProvider.Logger<Logger>(typeof(DdonWebServer));
        private readonly MailSetting _mailSetting;
        private readonly WebServerSetting _webServerSetting;

        public MailSend(MailSetting mailSetting, WebServerSetting webServerSettings)
        {
            _mailSetting = mailSetting ?? throw new ArgumentNullException(nameof(mailSetting));
            _webServerSetting = webServerSettings ?? throw new ArgumentNullException(nameof(webServerSettings));
        }
        public enum MailModel : byte
        {
            NewAccount = 0,
            MailVerify = 1,
            PasswordReset = 2,
        }
        public async Task SendAsync(MailModel mailModel, Account account, CancellationToken cancellationToken = default)
        {
            Dictionary<string, string> placeholders = new();

            string model = mailModel switch
            {
                MailModel.NewAccount => "new_account",
                MailModel.MailVerify => "mail_verify",
                MailModel.PasswordReset => "reset_password",
                _ => throw new InvalidOperationException("Unsupported mail model")
            };

            if (string.IsNullOrWhiteSpace(model))
            {
                Logger.Error("MailSend - Mail model not provided");
                return;
            }

            if (account == null)
            {
                Logger.Error("MailSend - Invalid account provided");
                return;
            }

            var templateFile = Path.Combine(_mailSetting.TemplatePath, $"{model}.html");

            if (!System.IO.File.Exists(templateFile))
            {
                Logger.Error($"MailSend - Template not found: {templateFile}");
                return;
            }

            var body = await System.IO.File.ReadAllTextAsync(templateFile, cancellationToken);
            var baseUrl = _mailSetting.DomainUrl?.TrimEnd('/') ?? throw new InvalidOperationException("ServerUrl is not configured");
            
            placeholders = new Dictionary<string, string>
            {
                ["{{LogoLink}}"] = _mailSetting.LogoLink,
                ["{{UserName}}"] = WebUtility.HtmlEncode(account.Name),
                ["{{DomainUrl}}"] = baseUrl,
                ["{{Schema}}"] = (bool)_mailSetting.IsHttps ? "https" : "http",
                ["{{Year}}"] = DateTime.UtcNow.Year.ToString()
            };

            if (mailModel == MailModel.NewAccount || mailModel == MailModel.MailVerify)
            {
                if (string.IsNullOrWhiteSpace(account.MailToken))
                {
                    Logger.Error("MailSend - A mail_token is required for this mail model");
                    return;
                }

                placeholders.Add("{{VerificationLink}}", $"{baseUrl}:{_webServerSetting.PublicWebEndPoint.Port}/web/mail_verify.html?token={Uri.EscapeDataString(account.MailToken)}");
            }
            else if(mailModel == MailModel.PasswordReset)
            {
                if (string.IsNullOrWhiteSpace(account.PasswordToken))
                {
                    Logger.Error("MailSend - A password_token is required for this mail model");
                    return;
                }

                placeholders.Add("{{PasswordResetLink}}", $"{baseUrl}:{_webServerSetting.PublicWebEndPoint.Port}/web/reset_password.html?token={Uri.EscapeDataString(account.PasswordToken)}");
            }
            else
            {
                Logger.Error($"MailSend - Unsupported mail model: {mailModel}");
                return;
            }

            foreach (var item in placeholders)
            {
                body = body.Replace(item.Key, item.Value);
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_mailSetting.FromAddress));
            message.To.Add(MailboxAddress.Parse(account.Mail));
            message.Subject = mailModel switch
            {
                MailModel.NewAccount => _mailSetting.SubjectNewAccount,
                MailModel.MailVerify => _mailSetting.SubjectVerifyEmail,
                MailModel.PasswordReset => _mailSetting.SubjectPasswordReset,
                _ => throw new InvalidOperationException("Unsupported mail subject")
            };

            message.Body = new TextPart("html")
            {
                Text = body
            };

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _mailSetting.SmtpServer,
                _mailSetting.SmtpPort,
                SecureSocketOptions.StartTls,
                cancellationToken
            );

            if (!string.IsNullOrWhiteSpace(_mailSetting.SmtpUser))
            {
                await smtpClient.AuthenticateAsync(
                    _mailSetting.SmtpUser,
                    _mailSetting.SmtpPassword,
                    cancellationToken
                );
            }

            await smtpClient.SendAsync(message, cancellationToken);
            await smtpClient.DisconnectAsync(true, cancellationToken);
        }
    }
}

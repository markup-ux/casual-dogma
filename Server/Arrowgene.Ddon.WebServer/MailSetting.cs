using System.IO;
using System.Runtime.Serialization;
using Arrowgene.Ddon.Shared;

namespace Arrowgene.Ddon.WebServer
{
    [DataContract]
    public class MailSetting
    {
        [DataMember(Order = 1)]
        public bool? MailRequired { get; set; }
        [DataMember(Order = 2)]
        public bool? IsHttps { get; set; }

        [DataMember(Order = 3)]
        public string DomainUrl { get; set; }

        [DataMember(Order = 4)]
        public string SmtpServer { get; set; }

        [DataMember(Order = 5)]
        public int SmtpPort { get; set; }

        [DataMember(Order = 6)]
        public string SmtpUser { get; set; }

        [DataMember(Order = 7)]
        public string SmtpPassword { get; set; }

        [DataMember(Order = 8)]
        public string FromAddress { get; set; }

        [DataMember(Order = 9)]
        public string TemplatePath { get; set; }

        [DataMember(Order = 10)]
        public string LogoLink { get; set; }

        [DataMember(Order = 11)]
        public string SubjectNewAccount { get; set; }

        [DataMember(Order = 12)]
        public string SubjectVerifyEmail { get; set; }

        [DataMember(Order = 13)]
        public string SubjectPasswordReset { get; set; }

        public MailSetting()
        {
            SetDefaultValues();
        }

        public MailSetting(MailSetting setting)
        {
            MailRequired = setting.MailRequired;
            IsHttps = setting.IsHttps;
            DomainUrl = setting.DomainUrl;
            SmtpServer = setting.SmtpServer;
            SmtpPort = setting.SmtpPort;
            SmtpUser = setting.SmtpUser;
            SmtpPassword = setting.SmtpPassword;
            FromAddress = setting.FromAddress;
            TemplatePath = setting.TemplatePath;
            LogoLink = setting.LogoLink;
            SubjectNewAccount = setting.SubjectNewAccount;
            SubjectVerifyEmail = setting.SubjectVerifyEmail;
            SubjectPasswordReset = setting.SubjectPasswordReset;
        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext context)
        {
            SetDefaultValues();
        }

        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            MailRequired ??= false;
            IsHttps ??= false;
            DomainUrl ??= "www.dd.on";
            SmtpServer ??= "smtp.dd.on";
            SmtpPort = SmtpPort == 0 ? 587 : SmtpPort;
            SmtpUser ??= "no-reply@dd.on";
            SmtpPassword ??= "p@55VV0rD";
            FromAddress ??= "no-reply@dd.on";
            TemplatePath ??= Path.Combine(Util.ExecutingDirectory(), "Files/mail_templates");
            LogoLink ??= "http://www.dd.on:52099/launcher/logo.png";
            SubjectNewAccount ??= "DDON - Welcome to Ddon!";
            SubjectVerifyEmail ??= "DDON - Verify your email address";
            SubjectPasswordReset ??= "DDON - Password reset request";
        }

        private void SetDefaultValues()
        {
            MailRequired = false;
            IsHttps = false;
            DomainUrl = "www.dd.on";
            SmtpServer = "smtp.dd.on";
            SmtpPort = 587;
            SmtpUser = "no-reply@dd.on";
            SmtpPassword = "p@55VV0rD";
            FromAddress = "no-reply@dd.on";
            TemplatePath = Path.Combine(Util.ExecutingDirectory(), "Files/mail_templates");
            LogoLink = "http://www.dd.on:52099/launcher/logo.png";
            SubjectNewAccount = "DDON - Welcome to Ddon!";
            SubjectVerifyEmail = "DDON - Verify your email address";
            SubjectPasswordReset = "DDON - Password reset request";
        }
    }
}

using ExcellentEmailExperience.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using Windows.Media.Protection.PlayReady;

namespace ExcellentEmailExperience.Model
{
    internal class MailKitHandler : IMailHandler
    {
        private string mailAddress;
        private string password;
        private ImapClient imapClient;
        private MailKit.Net.Smtp.SmtpClient smtpClient;

        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MailKitHandler(string mail, string secret, Model.ConnectionConfig config)
        {
            mailAddress = mail;
            password = secret;

            imapClient = new ImapClient();
            imapClient.ConnectAsync(config.MailServer, config.imapPort, true);
            imapClient.AuthenticateAsync(mailAddress, password);

            smtpClient = new MailKit.Net.Smtp.SmtpClient();
            smtpClient.ConnectAsync(config.MailServer, config.smtpPort, true);
            smtpClient.AuthenticateAsync(mailAddress, password);
        }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void Forward(MailContent content, List<MailAddress> NewTo)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<MailContent> GetFolder(string name, bool old, bool refresh, int count)
        {
            throw new NotImplementedException();
            var folder = imapClient.GetFolder(name);
            folder.Open(FolderAccess.ReadOnly);

            for (var i = 0; i < folder.Count; i++)
            {
                var message = folder.GetMessage(i);
            }

            yield break;
        }

        private MailContent BuildMailContent(MimeMessage message)
        {
            throw new NotImplementedException();
            MailContent mailContent = new();
            mailContent.MessageId = mailContent.ThreadId = message.MessageId;
            mailContent.date = message.Date.UtcDateTime;
            mailContent.from = (MailAddress) message.Sender;
            foreach (var address in message.To.Mailboxes)
            {
                mailContent.to.Add((MailAddress) address);
            }
            foreach (var address in message.Bcc.Mailboxes)
            {
                mailContent.bcc.Add((MailAddress) address);
            }
            foreach (var address in message.Cc.Mailboxes)
            {
                mailContent.cc.Add((MailAddress) address);
            }

            return mailContent;
        }



        public string[] GetFolderNames()
        {
            var personalNamespace = imapClient.PersonalNamespaces[0];
            var rootFolder = imapClient.GetFolder(personalNamespace);
            rootFolder.Open(FolderAccess.ReadOnly);

            List<string> folderNames = new List<string>();

            foreach (var subfolder in rootFolder.GetSubfolders(false))
            {
                folderNames.Add(subfolder.FullName);
            }

            return folderNames.ToArray();
        }

        public List<MailContent> Refresh(string name)
        {
            throw new NotImplementedException();
        }

        public void Reply(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void ReplyAll(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void Send(MailContent content)
        {
            var mimemessage = MailSetup.SetupMail(content);

            //TODO: plis impliment cancel token. please and thankyou. :) 
            smtpClient.SendAsync(mimemessage);
        }

       

        public void Reply(MailContent content, string Response)
        {
            throw new NotImplementedException();
        }

        public void ReplyAll(MailContent content, string Response)
        {
            throw new NotImplementedException();
        }

        public void TrashMail(string MessageId)
        {
            throw new NotImplementedException();
        }
    }
}

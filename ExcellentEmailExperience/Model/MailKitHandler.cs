using ExcellentEmailExperience.Interfaces;
using MailKit;
using MailKit.Net.Imap;
using System;
using System.Collections.Generic;
using System.Net.Mail;

namespace ExcellentEmailExperience.Model
{
    internal class MailKitHandler : IMailHandler
    {
        private string mailAddress;
        private string password;
        private ImapClient imapClient;
        private MailKit.Net.Smtp.SmtpClient smtpClient;

        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public MailKitHandler(string mail, string secret)
        {
            mailAddress = mail;
            password = secret;

            imapClient = new ImapClient();
            imapClient.ConnectAsync("imap.friends.com", 993, true);
            imapClient.AuthenticateAsync(mailAddress, password);

            smtpClient = new MailKit.Net.Smtp.SmtpClient();
            smtpClient.ConnectAsync("smtp.friends.com", 587, true);
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
            throw new NotImplementedException();
        }

        public void Reply(MailContent content, string Response)
        {
            throw new NotImplementedException();
        }

        public void ReplyAll(MailContent content, string Response)
        {
            throw new NotImplementedException();
        }
        
        public void DeleteMail(string MessageId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MailContent> Search(string query, int count)
        {
            throw new NotImplementedException();
        }

        public MailContent UpdateFlag(MailContent content, MailFlag flagtype)
        {
            throw new NotImplementedException();
        }
    }
}

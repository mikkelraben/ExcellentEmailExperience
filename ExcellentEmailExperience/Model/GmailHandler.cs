using ExcellentEmailExperience.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public class GmailHandler : IMailHandler
    {
        public MailAddress[] flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void Forward(MailContent content)
        {
            throw new NotImplementedException();
        }

        public MailContent[] GetInbox()
        {
            throw new NotImplementedException();
        }

        public MailContent NewMail(MailAddress reciever, string subject, MailAddress? CC = null, MailAddress? BCC = null, string? body = null, string? attach = null)
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
    }
}

using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Interfaces
{
    public interface IMailHandler
    {
        MailAddress[] FlaggedMails { get; set; }
        bool CheckSpam(MailContent content);
        void Forward(MailContent content);
        void Reply(MailContent content);
        void ReplyAll(MailContent content);
        void Send(MailContent content);
        MailContent NewMail(
                        MailAddress reciever,
                        string subject,
                        MailAddress? CC = null,
                        MailAddress? BCC = null,
                        string? body = null,
                        string? attach = null
                        );
        MailContent[] GetInbox();
    }
}

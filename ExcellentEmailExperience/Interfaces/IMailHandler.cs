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
        List<MailAddress> flaggedMails { get; set; }
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

        /// <summary>
        /// Retrieves the names for every mail folder the user has made.
        /// </summary>
        /// <returns>
        /// Array containing folder name strings.
        /// </returns>
        string[] GetFolderNames();

        /// <summary>
        /// Retrieves batch mails from a folder determined by <paramref name="name"/>. 
        /// Gets the newest mail if <paramref name="refresh"/> is true, 
        /// or else the next batch of mails is determined by <paramref name="old"/>.
        /// </summary>
        /// <param name="name"> Name of the current active folder that mail is retrieved from </param>
        /// <param name="old"> True if GetFolder should retrieve older mails, false if newer </param>
        /// <param name="refresh"> True if the mail list should restart at the newest mail </param>
        /// <returns>List containing MailContent, with an upper bound of 50? elements</returns>
        List<MailContent> GetFolder(string name, bool old, bool refresh);

        /// <summary>
        /// Refreshes the mail retrieved in all folders. Calls GetFolder with refresh = true.
        /// </summary>
        /// <param name="name"> Name of the current active folder that mail is retrieved from </param>
        /// <returns>List containing MailContent, with an upper bound of 50? elements</returns>
        List<MailContent> Refresh(string name);
    }
}

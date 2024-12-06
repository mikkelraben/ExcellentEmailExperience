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

        /// <summary>
        /// takes an email that you received and sends it to someone else with some modifications to indicate its a 
        /// forward
        /// </summary>
        /// <param name="content"></param> this is the mail that we received, the one we want to forward
        /// <param name="NewTo"></param> this is the list of people to whom we want to forward to. 
        void Forward(MailContent content, List<MailAddress> NewTo);
        void Reply(MailContent content, string Response);
        void ReplyAll(MailContent content, string Response);
        void Send(MailContent content);

        void TrashMail(string MessageId);

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
        /// <param name="count"> The number of mails to retrieve </param>
        /// <returns>IEnumerable containing MailContent, with an upper bound of 50? elements</returns>
        IEnumerable<MailContent> GetFolder(string name, bool old, bool refresh,int count);


        /// <summary>
        /// Searches for mails in the current folder where query follows googles q documentation:
        /// https://developers.google.com/gmail/api/reference/rest/v1/users.messages/list.
        /// </summary>
        /// <param name="query"> the search query after googles api </param>
        /// <param name="count"> the number of mails to retrieve </param>
        /// <returns> IEnumerable of mail suiting query</returns>
        IEnumerable<MailContent> Search(string query, int count);
    }
}

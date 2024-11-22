using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public enum BodyType
    {
        Plain,
        Html
    }

    public class MailContent
    {
        public MailAddress? from;
        public List<MailAddress> to = new();
        public List<MailAddress> bcc = new();
        public List<MailAddress> cc = new();
        public BodyType bodyType = BodyType.Plain;
        public string subject = "";
        public string body = "";

        // List of paths to attachments
        public List<string> attachments = new();
        public DateTime date = new();
        public string ThreadId = "";
        public string MessageId = "";

    }
}

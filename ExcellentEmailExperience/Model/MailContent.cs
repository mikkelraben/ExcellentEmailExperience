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
        public MailAddress from { get; set; }
        public MailAddress[] to { get; set; }
        public MailAddress[] bcc { get; set; }
        public MailAddress[] cc { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public BodyType bodyType;
        public string attach_path { get; set; }
        public string date { get; set; }

    }
}

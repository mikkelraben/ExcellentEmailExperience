using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public class MailContent
    {
        public MailAddress from;
        public MailAddress[] to;
        public MailAddress[] bcc;
        public MailAddress[] cc;
        public string subject;
        public string body;
        public string attach_path;
        public string date;

    }
}

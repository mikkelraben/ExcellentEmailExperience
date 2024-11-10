﻿using System;
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
        public MailAddress from;
        public List<MailAddress> to;
        public List<MailAddress> bcc;
        public List<MailAddress> cc;
        public BodyType bodyType;
        public string subject;
        public string body;
        public string attach_path;
        public DateTime date;
        public string ThreadId;

    }
}

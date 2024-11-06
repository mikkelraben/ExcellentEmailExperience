using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    internal partial class MailContentViewModel
    {
        public void Update(MailContent mailContent)
        {
            From = mailContent.from;
            To = mailContent.to;
            Bcc = mailContent.bcc;
            Cc = mailContent.cc;
            Subject = mailContent.subject;
            Body = mailContent.body;
            Attach_path = mailContent.attach_path;
            Date = mailContent.date;
            bodyType = mailContent.bodyType;
        }

        [ObservableProperty]
        public MailAddress from;

        [ObservableProperty]
        public List<MailAddress> to;

        [ObservableProperty]
        public List<MailAddress> bcc;

        [ObservableProperty]
        public List<MailAddress> cc;

        [ObservableProperty]
        public string subject;

        [ObservableProperty]
        public string body;
        public BodyType bodyType;

        [ObservableProperty]
        public string attach_path;

        [ObservableProperty]
        public string date;

    }
}

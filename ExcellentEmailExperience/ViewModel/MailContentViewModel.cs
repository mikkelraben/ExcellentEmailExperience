using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Net.Mail;

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
            Attachments = mailContent.attachments;
            Date = mailContent.date;
            bodyType = mailContent.bodyType;
            messageId = mailContent.MessageId;
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
        public List<string> attachments;

        [ObservableProperty]
        public DateTime date;

        [ObservableProperty]
        public bool isEditable = false;

        public string messageId;

    }
}

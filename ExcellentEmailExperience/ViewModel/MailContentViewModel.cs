using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Helpers;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Mail;
using Windows.Storage;

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

            Date = mailContent.date;
            bodyType = mailContent.bodyType;
            messageId = mailContent.MessageId;
            Unread = mailContent.flags.HasFlag(MailFlag.unread);

            recipients.Clear();

            attachments.Clear();
            mailContent.attachments.ForEach(x =>
            {

                BitmapImage bitmapImage = ThumbnailFromPath.GetThumbnailFromPath(x);

                attachments.Add(new AttachmentViewModel { Path = x, Preview = bitmapImage });
            });


            mailContent.to.ForEach(x => recipients.Add(new(x.Address)));
        }



        [ObservableProperty]
        public MailAddress from;

        [ObservableProperty]
        public List<MailAddress> to;

        // This variable is used to store the strings of the emails so they can be edited
        public ObservableCollection<StringWrapper> recipients = new();
        public ObservableCollection<StringWrapper> ccStrings = new();
        public ObservableCollection<StringWrapper> bccStrings = new();

        [ObservableProperty]
        public List<MailAddress> bcc;

        [ObservableProperty]
        public List<MailAddress> cc;

        [ObservableProperty]
        public string subject;

        [ObservableProperty]
        public string body;
        public BodyType bodyType;

        public ObservableCollection<AttachmentViewModel> attachments = new();

        [ObservableProperty]
        public DateTime date;

        [ObservableProperty]
        public bool isEditable = false;

        [ObservableProperty]
        public bool unread = false;

        public string messageId;

    }

    /// <summary>
    /// Worry not this is a terrible class to have but due to Microsoft's lack of support for their own wonderful technology(binding) I have to create this class to be able to edit the recipients of the emails
    /// </summary>
    [ObservableObject]
    public partial class StringWrapper
    {
        public StringWrapper(string value)
        {
            this.Value = value;
        }
        [ObservableProperty]
        public string value;
    }

    public class AttachmentViewModel
    {
        public string Path { get; set; }
        public BitmapImage Preview { get; set; }
        public bool Editable = false;
        public string name;
    }
}

using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;

namespace ExcellentEmailExperience.Model
{
    public class GmailHandler : IMailHandler
    {
        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private UserCredential userCredential;
        private GmailService service;


        public GmailHandler(UserCredential credential)
        {
            userCredential = credential;

            service = new GmailService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = userCredential,
                ApplicationName = "ExcellentEmailExperience",
            });
        }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void Forward(MailContent content)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MailContent> GetFolder(string name, bool old, bool refresh)
        {
            if (name == "Inbox")
            {
                IList<Google.Apis.Gmail.v1.Data.Message> messages = service.Users.Messages.List("me").Execute().Messages;

                foreach (var message in messages)
                {
                    var msg = service.Users.Messages.Get("me", message.Id).Execute();
                    MailContent mailContent = new();


                    //Change all this to support e-boks messages / other weird message types
                    switch (msg.Payload.MimeType)
                    {
                        case "text/plain":
                            mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(msg.Payload.Body.Data.Replace('-', '+').Replace('_', '/')));
                            mailContent.bodyType = BodyType.Plain;
                            break;
                        case "text/html":
                            mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(msg.Payload.Body.Data.Replace('-', '+').Replace('_', '/')));
                            mailContent.bodyType = BodyType.Html;
                            break;
                        case "multipart/alternative":

                            bool containsHtml = false;
                            foreach (var part in msg.Payload.Parts)
                            {
                                if (part.MimeType == "text/html")
                                {
                                    containsHtml = true;
                                    break;
                                }
                            }

                            foreach (var part in msg.Payload.Parts)
                            {
                                switch (part.MimeType)
                                {
                                    case "text/plain":
                                        if (containsHtml)
                                        {
                                            continue;
                                        }
                                        mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(part.Body.Data.Replace('-', '+').Replace('_', '/')));
                                        mailContent.bodyType = BodyType.Plain;
                                        break;
                                    case "text/html":
                                        mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(part.Body.Data.Replace('-', '+').Replace('_', '/')));
                                        mailContent.bodyType = BodyType.Html;
                                        break;
                                }
                            }
                            break;
                        default:
                            //TODO: Emit a warning
                            break;

                    }

                    foreach (var header in msg.Payload.Headers)
                    {
                        if (header.Name == "From")
                        {
                            mailContent.from = new MailAddress(header.Value);
                        }
                        else if (header.Name == "To")
                        {
                            mailContent.to = [new MailAddress(header.Value)];
                        }
                        else if (header.Name == "Subject")
                        {
                            mailContent.subject = header.Value;
                        }
                        else if (header.Name == "Date")
                        {
                            DateTimeOffset date;
                            MimeKit.Utils.DateUtils.TryParse(header.Value, out date);
                            mailContent.date = date.ToString();
                        }
                    }
                    yield return mailContent;

                }
                yield break;

            }
            else
            {
                throw new NotImplementedException();

            }
        }


        public string[] GetFolderNames()
        {
            return ["Inbox", "Sent", "Drafts", "Spam", "Trash"];
        }

        public MailContent NewMail(MailAddress reciever, string subject, MailAddress? CC = null, MailAddress? BCC = null, string? body = null, string? attach = null)
        {
            throw new NotImplementedException();
        }

        public List<MailContent> Refresh(string name)
        {
            throw new NotImplementedException();
        }

        public void Reply(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void ReplyAll(MailContent content)
        {
            throw new NotImplementedException();
        }

        public void Send(MailContent content)
        {
            throw new NotImplementedException();
        }
    }
}

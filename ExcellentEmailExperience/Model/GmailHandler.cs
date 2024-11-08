using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.ViewModel;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using System;
using System.Collections.Generic;
using System.Net.Mail;

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
                            mailContent.date = header.Value;
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

using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;

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
            var profileRequest = service.Users.GetProfile("me");
            var message = new MailMessage
            {
                Subject = content.subject,
                From = content.from
            };
            foreach (var recipient in content.to)
            {
                message.To.Add(recipient);
            }

            var MessageContent = AlternateView.CreateAlternateViewFromString(content.body, new System.Net.Mime.ContentType("text/html"));
            MessageContent.ContentType.CharSet = Encoding.UTF8.WebName;

            // this here adds an attachment. but idk if i need to pass the path
            // as the string or if i have to do some file conversion

            if(content.attach_path != "")
            {
                Attachment pdfAttachment = new Attachment(content.attach_path);
                pdfAttachment.ContentType = new System.Net.Mime.ContentType("application/pdf");
                message.Attachments.Add(pdfAttachment);
            }

            
            message.AlternateViews.Add(MessageContent);

            var mimemessage = MimeMessage.CreateFromMailMessage(message);

            using (var memoryStream = new MemoryStream())
            {
                mimemessage.WriteTo(memoryStream);
                var rawMessage = memoryStream.ToArray();

                var encodedMessage = Convert.ToBase64String(rawMessage)
                    .Replace('+','-')
                    .Replace('/', '_')
                    .Replace("=", "");

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = encodedMessage
                };
                var sendRequest = service.Users.Messages.Send(gmailMessage, "me");
                sendRequest.Execute();
            }

        }
    }
}

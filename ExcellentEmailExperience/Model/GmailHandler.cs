using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using MimeKit;
using Org.BouncyCastle.Asn1.Cmp;
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

        public void Forward(MailContent content, List<MailAddress> NewTo)
        {
            var Mail = new MailContent();
            Mail.body = "Forwarded from " + content.from.ToString() + "\n " + content.body + " \n\n Originally sent to:" + content.to.ToString();
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            Mail.from = new MailAddress(user.EmailAddress);
            Mail.to = NewTo;
            Send(Mail);

            //TODO: Set the from field to the emailAddress of GmailAccount
            //throw new NotImplementedException();
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

            var mail = new MailContent();
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            mail.from = new MailAddress(user.EmailAddress);
            mail.to.Add(reciever); // we might need to change the reciver to a list and not just a mailcontent. cause what if you're sending to more people.
            mail.bcc.Add(BCC);
            mail.cc.Add(CC);
            mail.subject = subject;
            mail.body = body;
            mail.attach_path = attach;
            mail.date = System.DateTime.Now.ToString();

            //throw new NotImplementedException();

            return mail;
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
            var MessageContent = AlternateView.CreateAlternateViewFromString("");
            if (content.bodyType == BodyType.Html)
            {
                MessageContent = AlternateView.CreateAlternateViewFromString(content.body, new System.Net.Mime.ContentType("text/html"));

            }
            if (content.bodyType == BodyType.Plain)
            {
                MessageContent = AlternateView.CreateAlternateViewFromString(content.body, new System.Net.Mime.ContentType("text/plain"));

            }
            MessageContent.ContentType.CharSet = Encoding.UTF8.WebName;

            // this here adds an attachment. but idk if i need to pass the path
            // as the string or if i have to do some file conversion

            if (content.attach_path != "")
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
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = encodedMessage
                };
                var sendRequest = service.Users.Messages.Send(gmailMessage, "me");
                sendRequest.Execute();
            }

            //add mailcontent to 'sent' mails folder. 

        }
    }
}

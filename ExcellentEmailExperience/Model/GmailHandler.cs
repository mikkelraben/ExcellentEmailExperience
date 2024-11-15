﻿using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Views;
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
using System.Runtime.Caching;
using Windows.Storage.Pickers;

namespace ExcellentEmailExperience.Model
{
    public class GmailHandler : IMailHandler
    {
        private ObjectCache cache;
        private double cacheTTL;
        private string? oldCacheKey;

        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private UserCredential userCredential;
        private GmailService service;


        public GmailHandler(UserCredential credential) //, ObjectCache cache, double TTL)
        {
            //this.cache = cache;
            //cacheTTL = TTL;
            cache = MemoryCache.Default;
            cacheTTL = 30;
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

        // this function creates a new mailcontent and sends this one
        // so the original message wont be modified. 
        public void Forward(MailContent content, List<MailAddress> NewTo)
        {
            var Mail = new MailContent();
            Mail.subject = "Forward: " + Mail.subject;
            Mail.body = "Forwarded from " + content.from.ToString() + "\n " + content.body + " \n\n Originally sent to:" + content.to.ToString();
            
            //making the currect account the sender. 
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            Mail.from = new MailAddress(user.EmailAddress);

            //changes the receiver to the person who is being forwarded to. 
            Mail.to = NewTo;
            Send(Mail);

            //TODO: Set the from field to the emailAddress of GmailAccount
            //throw new NotImplementedException();
        }

        public IEnumerable<MailContent> GetFolder(string name, bool old, bool refresh)
        {
            string allcaps = name.ToUpper();

            var request = service.Users.Messages.List("me");
            request.LabelIds = allcaps;
            IList<Google.Apis.Gmail.v1.Data.Message> messages = request.Execute().Messages;

            if(messages == null)
            {
                yield break;
            }

            string CacheKey = messages[0].Id;

            // retrieve mail from cache if it is up to date
            if (CacheKey == oldCacheKey)
            {
                yield return (MailContent)cache.Get(CacheKey);
            }

            foreach (var message in messages)
            {
                var msg = service.Users.Messages.Get("me", message.Id).Execute();
                MailContent mailContent = new();
                mailContent.ThreadId = msg.ThreadId;

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
                        Console.WriteLine("cry");
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
                        mailContent.date = date.UtcDateTime;
                    }
                }

                // cache the retrieved mailcontent and delete old cache
                cache.Set(CacheKey, mailContent, DateTimeOffset.Now.AddMinutes(cacheTTL));
                if (oldCacheKey != null)
                {
                    cache.Remove(oldCacheKey);
                }
                oldCacheKey = CacheKey;

                yield return mailContent;

            }
            yield break;
        }


        public string[] GetFolderNames()
        {
            var labelsListRequest = service.Users.Labels.List("me");
            IList<Label> labels = (labelsListRequest.Execute()).Labels;
            List<string> labelNames = new List<string>();

            if (labels != null && labels.Count > 0)
            {
                foreach (var labelItem in labels)
                {
                    labelNames.Add(labelItem.Name);
                }
            }

            string[] labelString = labelNames.ToArray();

            return labelString;
        }

        public MailContent NewMail(MailAddress reciever, string subject, MailAddress? CC = null, MailAddress? BCC = null, string? body = null, string? attach = null)
        {

            var mail = new MailContent();
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            mail.from = new MailAddress(user.EmailAddress);
            mail.to.Add(reciever); // we might need to change the receiver to a list and not just a mailcontent. cause what if you're sending to more people.
            mail.bcc.Add(BCC); 
            mail.cc.Add(CC);
            mail.subject = subject;
            mail.body = body;
            mail.attach_path = attach;
            
            //throw new NotImplementedException();

            return mail;
        }

        public List<MailContent> Refresh(string name)
        {
            throw new NotImplementedException();
        }

        // when calling reply. it is important that you give it the exact mailcontent you want to reply to
        // dont change the (to) and (from) fields beforehand, the code will handle that for you. you can change the body. 
        public void Reply(MailContent content)
        {
            MailContent reply = new MailContent();
            reply = content;
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            reply.from = new MailAddress(user.EmailAddress);
            reply.subject = "Re: " + reply.subject;
            Send(reply);
        }

        public void ReplyAll(MailContent content)
        {
            MailContent reply = new MailContent();
            reply = content;
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            reply.to.AddRange(content.to);
            var profileRequest = service.Users.GetProfile("me");
            var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
            reply.from = new MailAddress(user.EmailAddress);

            reply.to.Remove(reply.from);
            reply.subject = "Re: " + reply.subject;
            Send(reply);
            throw new NotImplementedException();
        }

        public void Send(MailContent content)
        {
            content.date = DateTime.Now;
            // creates a new mailmessage object, these are the ones that we need to setup before sending
            var message = new MailMessage
            {
                Subject = content.subject,
                From = content.from
            };

            // adds bcc,cc,and recipient.
            foreach (var recipient in content.to)
            {
                message.To.Add(recipient);
            }
            foreach (var recipient in content.bcc)
            {
                message.Bcc.Add(recipient);
            }
            foreach(var recipient in content.cc)
            {
                message.CC.Add(recipient);
            }

            // this part creates the main text to the message as either plaintext or html
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

            //after creating the maintext we need to add it to the mailmessage object
            message.AlternateViews.Add(MessageContent);
            
            // convert to mimemessage, this is necessary for sending
            var mimemessage = MimeMessage.CreateFromMailMessage(message);

            // making memory stream. 
            using (var memoryStream = new MemoryStream())
            {

                // we need to convert our message to a base64 string. 
                mimemessage.WriteTo(memoryStream);
                var rawMessage = memoryStream.ToArray();

                var encodedMessage = Convert.ToBase64String(rawMessage)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");

                // convert to gmailMessage
                var gmailMessage = new Google.Apis.Gmail.v1.Data.Message
                {
                    Raw = encodedMessage
                };
                // apply the thread id, in case this is a reply. 
                // if you are just calling send, and not from reply. the ThreadId should be 0
                // it will  be given an id by google when sending.
                // this is also why we shouldnt give it a threadid in the newmail function
                if(content.ThreadId != null)
                {
                    gmailMessage.ThreadId = content.ThreadId;
                }
                
                // send it.
                var sendRequest = service.Users.Messages.Send(gmailMessage, "me");
                sendRequest.Execute();
            }
        }
    }
}

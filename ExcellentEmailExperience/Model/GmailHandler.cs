using ExcellentEmailExperience.Interfaces;
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
using Windows.Storage.Pickers;
using System.Diagnostics;
using WinUIEx.Messaging;
using Windows.Storage;

namespace ExcellentEmailExperience.Model
{
    public class GmailHandler : IMailHandler
    {
        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private UserCredential userCredential;
        private GmailService service;
        private MailAddress mailAddress;


        public GmailHandler(UserCredential credential, MailAddress mailAddress)
        {
            userCredential = credential;
            this.mailAddress = mailAddress;

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
            Mail.body = $"Forwarded from {content.from}\n {content.body} \n\n Originally sent to:{content.to}";

            //making the currect account the sender. 
            Mail.from = mailAddress;

            //changes the receiver to the person who is being forwarded to. 
            Mail.to = NewTo;
            Send(Mail);
        }

        public IEnumerable<MailContent> GetFolder(string name, bool old, bool refresh)
        {
            var request = service.Users.Messages.List("me");
            request.LabelIds = name;
            IList<Google.Apis.Gmail.v1.Data.Message> messages = request.Execute().Messages;

            if (messages == null)
            {
                yield break;
            }

            foreach (var message in messages)
            {
                var msg = service.Users.Messages.Get("me", message.Id).Execute();
                MailContent mailContent = BuildMailContent(msg);

                yield return mailContent;

            }
            yield break;
        }



        private MailContent BuildMailContent(Google.Apis.Gmail.v1.Data.Message msg)
        {
            MailContent mailContent = new();
            mailContent.MessageId = msg.Id;
            mailContent.ThreadId = msg.ThreadId;

            HandleMessagePart(msg.Payload, mailContent);

            foreach (var header in msg.Payload.Headers)
            {
                if (header.Name == "From")
                {
                    mailContent.from = new MailAddress(header.Value);
                }
                else if (header.Name == "To")
                {
                    foreach (var address in header.Value.Split(','))
                    {
                        mailContent.to.Add(new MailAddress(address));
                    }
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

            return mailContent;
        }

        private void HandleMessagePart(Google.Apis.Gmail.v1.Data.MessagePart messagePart, MailContent mailContent)
        {
            // Sort by MIME type
            if (messagePart.MimeType.StartsWith("multipart/"))
            {
                foreach (var part in messagePart.Parts)
                {
                    HandleMessagePart(part, mailContent);
                }
            }
            else if (messagePart.MimeType == "text/plain")
            {
                if (mailContent.bodyType == BodyType.Plain)
                {
                    mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(messagePart.Body.Data.Replace('-', '+').Replace('_', '/')));
                }
            }
            else if (messagePart.MimeType == "text/html")
            {
                if (mailContent.bodyType == BodyType.Plain)
                {
                    mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(messagePart.Body.Data.Replace('-', '+').Replace('_', '/')));
                    mailContent.bodyType = BodyType.Html;
                }
            }
            else if (messagePart.MimeType.StartsWith("image/"))
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;

                var extension = messagePart.MimeType.Split('/')[1];

                switch (extension)
                {
                    case "svg+xml":
                        extension = "svg";
                        break;
                    case "vnd.microsoft.icon":
                        extension = "ico";
                        break;
                }

                var fileName = messagePart.Filename == "" ? $"{messagePart.PartId}.{extension}" : messagePart.Filename;

                var cid = "";

                foreach (var header in messagePart.Headers)
                {
                    switch (header.Name.ToLower())
                    {
                        case "content-id":
                            cid = header.Value.Trim(['<', '>']);
                            if (cid == "")
                                break;
                            cid = Convert.ToHexString(Encoding.UTF8.GetBytes(cid));
                            cid = folder.Path + $"\\attachments\\{mailContent.MessageId}\\{cid}";
                            break;
                    }
                }



                var filePath = folder.Path + $"\\attachments\\{mailContent.MessageId}\\{fileName}";

                mailContent.attachments.Add(filePath);

                if (File.Exists(filePath))
                {
                    return;
                }
                var attachment = service.Users.Messages.Attachments.Get("me", mailContent.MessageId, messagePart.Body.AttachmentId).Execute();
                var attachmentData = Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/'));

                Directory.CreateDirectory(folder.Path + $"\\attachments\\{mailContent.MessageId}");
                File.WriteAllBytes(filePath, attachmentData);
                if (cid != "")
                    File.CreateSymbolicLink(cid, $".\\{fileName}");
            }
            else if (messagePart.MimeType.StartsWith("application/"))
            {
            }
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
                    // TODO: maybe use labelNames.Add(labelItem.Id); but we will think about this after implementing mailkit
                    labelNames.Add(labelItem.Name);
                }
            }

            return labelNames.ToArray();
        }

        public List<MailContent> Refresh(string name)
        {
            throw new NotImplementedException();
        }

        // when calling reply. it is important that you give it the exact mailcontent you want to reply to
        // dont change the (to) and (from) fields beforehand, the code will handle that for you. you can change the body. 

        // call this with the mailcontent currently being displayed. should only be called when a mail is displayed
        public void Reply(MailContent content, string Response)
        {
            MailContent reply = new MailContent();
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            reply.from = mailAddress;
            reply.subject = "Re: " + content.subject;
            reply.body = Response;
            Send(reply);
        }

        // call this with the mailcontent currently being displayed. should only be called when a mail is displayed
        public void ReplyAll(MailContent content, string Response)
        {
            MailContent reply = new MailContent();
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            reply.to.AddRange(content.to);
            reply.from = mailAddress;
            reply.body = Response;
            reply.to.Remove(reply.from);
            reply.subject = "Re: " + content.subject;
            Send(reply);
            //throw new NotImplementedException();
        }

        public void Send(MailContent content)
        {
            if(content.to.Count == 0)
            {
                throw new Exception("no recipient");
            }
            if(content.subject == "")
            {
                throw new Exception("no subject");
            }

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

            // this is bad but we will fix later. this is only for testing purposes. 
            try
            {
                foreach (var recipient in content.bcc)
                {
                    message.Bcc.Add(recipient);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error in bcc field" + ex);
            }
            try
            {
                foreach (var recipient in content.cc)
                {
                    message.CC.Add(recipient);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error in cc field" + ex);
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

            //if (content.attach_path != "")
            //{
            //    Attachment pdfAttachment = new Attachment(content.attach_path);
            //    pdfAttachment.ContentType = new System.Net.Mime.ContentType("application/pdf");
            //    message.Attachments.Add(pdfAttachment);
            //}

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
                if (content.ThreadId != null)
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

using ExcellentEmailExperience.Interfaces;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using MimeKit.Tnef;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Web.Http;
using static ExcellentEmailExperience.Model.GmailHandler;
using static Google.Apis.Requests.BatchRequest;

namespace ExcellentEmailExperience.Model
{
    public class GmailHandler : IMailHandler
    {
        private Dictionary<MailFlag, string> Flag2Label = new Dictionary<MailFlag, string>() {
            { MailFlag.unread, "UNREAD" },
            { MailFlag.favorite, "STARRED" },
            { MailFlag.spam, "SPAM" },
            { MailFlag.trash, "TRASH" }
        };
        private Dictionary<string, MailFlag> Label2Flag = new Dictionary<string, MailFlag>()
        {
            { "UNREAD", MailFlag.unread },
            { "STARRED", MailFlag.favorite },
            { "SPAM", MailFlag.spam },
            { "TRASH", MailFlag.trash }
        };

        public struct Mail{
            public MailContent email;
            public bool Deletion;
        };

        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private UserCredential userCredential;
        private GmailService service;
        private MailAddress mailAddress;
        public ulong NewestId;
        

        /// <summary>
        /// Adds the option to disable cache if it is run on the TestServer. Cache disabled if TestServer == true.
        /// </summary>
        private bool TestServer;

        // modifies the body string so that google doesn't shit itself in fear and panic
        // and therefore modifies the message to fit its asinine standards
        public string MakeDaddyGHappy(string body)
        {
            body = body.Replace("\n", "\r\n");
            body = body.Replace(" \r", "\r");
            body += "\r\n";

            return body;
        }
        private CacheHandler? cache;

        public GmailHandler(UserCredential credential, MailAddress mailAddress, bool testServer = false)
        {
            userCredential = credential;
            this.mailAddress = mailAddress;

            service = new GmailService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = userCredential,
                ApplicationName = "ExcellentEmailExperience",
            });

            TestServer = testServer;
            if (!testServer)
            {
                cache = new CacheHandler(mailAddress.Address);
            }
        }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }
        public IEnumerable<MailContent> FullSync(string name, int count)
        {

            var request = service.Users.Messages.List("me");
            request.LabelIds = name;

            IList<Google.Apis.Gmail.v1.Data.Message> messages = request.Execute().Messages;
            if (messages == null)
            {
                yield break;
            }

            if (!TestServer && Label2Flag.ContainsKey(name))
            {
                var idList = messages.Select(message => message.Id).Distinct().ToList();
                cache.UpdateFolder(idList, name, Label2Flag, "INBOX");
            }

            foreach (var message in messages)
            {
                if (!TestServer && cache.CheckCache(message.Id))
                {
                    cache.AddFolder(message.Id, name, Label2Flag, "INBOX");
                }
                else
                {
                    var msg = service.Users.Messages.Get("me", message.Id).Execute();
                    MailContent mailContent = BuildMailContent(msg, name);

                    if (!TestServer)
                    {
                        cache.CacheMessage(mailContent, name);
                    }

                }
            }

            for (int i = 0; i < count; i++)
            {
                yield return cache.GetCache(messages[i].Id);
            }
            var NewestMessage = service.Users.Messages.Get("me", messages[0].Id).Execute();
            NewestId = NewestMessage.HistoryId.Value;

            yield break;
        }

        public IEnumerable<Mail> PartialSync(string folderName)
        {
            foreach (var label in GetFolderNames())
            {

                var refreq = service.Users.History.List("me");
                refreq.StartHistoryId = NewestId;
                var historyResponse = refreq.Execute();

                if (historyResponse.History == null)
                {
                    yield break;
                }

                foreach (var history in historyResponse.History)
                {
                    if (history.MessagesAdded == null)
                    {
                        yield break;
                    }

                    if (!TestServer && Label2Flag.ContainsKey(label))
                    {
                        var idList = history.MessagesAdded.Select(message => message.Message.Id).Distinct().ToList();
                        cache.UpdateFolder(idList, label, Label2Flag, "INBOX");
                    }

                    foreach (var addedMessage in history.MessagesAdded)
                    {
                        if (!TestServer && cache.CheckCache(addedMessage.Message.Id))
                        {
                            Mail mailStruct = new Mail();
                            mailStruct.email = cache.GetCache(addedMessage.Message.Id);
                            mailStruct.Deletion = false;

                            yield return mailStruct;
                        }
                        else
                        {
                            var msg = service.Users.Messages.Get("me", addedMessage.Message.Id).Execute();

                            // im just going to assume that when you get a new mail that hasnt been seen before that its also unread. 
                            MailContent mailContent = BuildMailContent(msg, label);

                            Mail mailStruct = new Mail();
                            mailStruct.email = mailContent;
                            mailStruct.Deletion = false;

                            if (!TestServer)
                            {
                                cache.CacheMessage(mailContent, label);
                            }

                            yield return mailStruct;
                        }
                    }
                    foreach (var deletedMessage in history.MessagesDeleted)
                    {
                        if (cache.CheckCache(deletedMessage.Message.Id))
                        {
                            cache.ClearRow(deletedMessage.Message.Id);
                        }
                        MailContent mailContent = new MailContent();
                        mailContent.MessageId = deletedMessage.Message.Id;
                        Mail mailStruct = new Mail();
                        mailStruct.email = mailContent;
                        mailStruct.Deletion = true;
                        yield return mailStruct;
                    }
                }
                yield break;
            }
        }

        public IEnumerable<Mail> Refresh(bool old, int count)
        {
            
        }

        public IEnumerable<MailContent> GetFolder(string name, int count)
        {
            bool fullSync = true;
            int counter = 0;
            try 
            {   
                foreach (var mail in PartialSync(name))
                {
                    counter++;
                    if (!mail.Deletion)
                    {
                        MailContent newMail = new MailContent();
                        newMail = mail.email;
                        yield return newMail;
                    }
                }
                fullSync = false;
            }
            finally { }

            if (fullSync)
            {
                foreach (var mail in FullSync(name,count))
                {
                    yield return mail;
                }
            }
            else
            {
                for (int i = counter; i < count; i++)
                {
                    
                    
                }
            }
        }

        private MailContent BuildMailContent(Google.Apis.Gmail.v1.Data.Message msg, string folderName)
        {
            MailContent mailContent = new();
            mailContent.MessageId = msg.Id;
            mailContent.ThreadId = msg.ThreadId;

            if (Label2Flag.ContainsKey(folderName))
            {
                mailContent.flags = Label2Flag[folderName];
            }

            try
            {
                HandleMessagePart(msg.Payload, mailContent);
            }
            catch (Exception e)
            {
                MessageHandler.AddMessage($"Error parsing message: {e.Message}", MessageSeverity.Error);
                throw;
            }
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
                    if (messagePart.Body.Data != null)
                    {
                        mailContent.body = Encoding.UTF8.GetString(Convert.FromBase64String(messagePart.Body.Data.Replace('-', '+').Replace('_', '/')));
                        mailContent.bodyType = BodyType.Html;
                    }
                }
            }
            else if (messagePart.MimeType.StartsWith("image/"))
            {
                string path;
                try
                {

                    StorageFolder folder = ApplicationData.Current.LocalFolder;
                    path = folder.Path;

                }
                catch (Exception)
                {
                    path = Directory.GetCurrentDirectory();
                }
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

                var fileName = messagePart.Filename == "" ? $"Attachment{messagePart.PartId}.{extension}" : messagePart.Filename;

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
                            cid = path + $"\\attachments\\{mailContent.MessageId}\\{cid}";
                            break;
                    }
                }

                var filePath = path + $"\\attachments\\{mailContent.MessageId}\\{fileName}";

                mailContent.attachments.Add(filePath);

                if (File.Exists(filePath))
                {
                    return;
                }
                var attachment = service.Users.Messages.Attachments.Get("me", mailContent.MessageId, messagePart.Body.AttachmentId).Execute();
                var attachmentData = Convert.FromBase64String(attachment.Data.Replace('-', '+').Replace('_', '/'));

                Directory.CreateDirectory(path + $"\\attachments\\{mailContent.MessageId}");
                File.WriteAllBytes(filePath, attachmentData);
                try
                {
                    if (!File.Exists(cid))
                    {
                        if (cid != "")
                            File.CreateSymbolicLink(cid, $".\\{fileName}");
                    }
                }
                catch (Exception)
                {

                }
            }
            else if (messagePart.MimeType.StartsWith("application/"))
            {
            }
            if (mailContent.body.EndsWith("\r\n"))
            {
                mailContent.body = mailContent.body.Remove(mailContent.body.Length - 2);
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
                    // TODO: maybe use labelItem.Id instead of labelItem.Name but we will think about this after implementing mailkit
                    string folderName = labelItem.Name;
                    labelNames.Add(folderName);
                }
            }

            // Sort the inbox to the top
            labelNames.Remove("INBOX");
            labelNames.Insert(0, "INBOX");

            return labelNames.ToArray();
        }

        public void Send(MailContent content)
        {

            content.body = MakeDaddyGHappy(content.body);

            var mimemessage = MailSetup.SetupMail(content);

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

        public void DeleteMail(string MessageId)
        {
            try
            {
                service.Users.Messages.Delete("me", MessageId);
                
                if (!TestServer)
                {
                    cache.ClearRow(MessageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error deleting email:" + ex.ToString());
            }

        }

        public IEnumerable<MailContent> Search(string query, int count)
        {
            var request = service.Users.Messages.List("me");
            request.Q = query;
            request.MaxResults = count;
            IList<Google.Apis.Gmail.v1.Data.Message> messages = request.Execute().Messages;

            if (messages == null)
            {
                yield break;
            }

            foreach (var message in messages)
            {
                if (!TestServer && cache.CheckCache(message.Id))
                {
                    yield return cache.GetCache(message.Id);
                }
                else
                {
                    var msg = service.Users.Messages.Get("me", message.Id).Execute();
                    MailContent mailContent = BuildMailContent(msg, "Search");

                    if (!TestServer)
                    {
                        cache.CacheMessage(mailContent, "Search");
                    }

                    yield return mailContent;
                }
            }
            yield break;
        }

        public MailContent Forward(MailContent content)
        {
            var Mail = new MailContent();
            Mail.subject = "Forward: " + content.subject;

            string to = "";
            foreach (var address in content.to)
            {
                to += address.Address + ", ";
            }

            to = to.Remove(to.Length - 2);

            Mail.body = $"Forwarded from {content.from}\n {content.body} \n\n Originally sent to:{to}";

            //making the currect account the sender. 
            Mail.from = mailAddress;
            Mail.ThreadId = content.ThreadId;
            return Mail;
        }

        public MailContent Reply(MailContent content)
        {
            MailContent reply = new MailContent();
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            reply.from = mailAddress;
            reply.body = content.body;
            reply.bodyType = content.bodyType;
            reply.subject = "Re: " + content.subject;
            return reply;
        }

        public MailContent ReplyAll(MailContent content)
        {
            MailContent reply = new MailContent();
            reply.ThreadId = content.ThreadId;
            reply.to = new List<MailAddress> { content.from };
            reply.to.AddRange(content.to);
            reply.from = mailAddress;
            reply.to.Remove(reply.from);
            reply.body = content.body;
            reply.bodyType = content.bodyType;
            reply.subject = "Re: " + content.subject;
            return reply;
        }

        public MailContent UpdateFlag(MailContent content, MailFlag flagtype)
        {
            var request = new ModifyMessageRequest();
            var folder = Flag2Label[flagtype];

            if (content.flags.HasFlag(flagtype))
            {
                request.RemoveLabelIds = new List<string>() { Flag2Label[flagtype] };
                var modifyRequest = service.Users.Messages.Modify(request, "me", content.MessageId);
                modifyRequest.Execute();

                content.flags &= ~flagtype;

                if (!TestServer)
                {
                    cache.RemoveFolder(content.MessageId, folder, Label2Flag, "INBOX");
                }
            }
            else
            {
                request.AddLabelIds = new List<string>() { Flag2Label[flagtype] };
                var modifyRequest = service.Users.Messages.Modify(request, "me", content.MessageId);
                modifyRequest.Execute();

                content.flags |= flagtype;

                if (!TestServer)
                {
                    cache.AddFolder(content.MessageId, folder, Label2Flag, "INBOX");
                }
            }

            return content;
        }
    }
}

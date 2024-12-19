using ExcellentEmailExperience.Interfaces;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using MimeKit.Tnef;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Web.Http;
using WinUIEx.Messaging;
using static ExcellentEmailExperience.Interfaces.IMailHandler;
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

        private UserCredential userCredential;
        private GmailService service;
        private MailAddress mailAddress;
        CancellationToken appClose;

        [JsonInclude]
        private ulong NewestId;

        [JsonInclude]
        private List<string> fullSyncedFolders = new();

        private ulong LatestId;
        private Mutex mutex = new Mutex();

        // modifies the body string so that google doesn't shit itself in fear and panic
        // and therefore modifies the message to fit its asinine standards
        public string MakeDaddyGHappy(string body)
        {
            // regex magic to replace "\n" with "\r\n" if it does not already have a preceding "\r"
            body = Regex.Replace(body, @"(?<!\r)\n", "\r\n");
            body = body.Replace(" \r", "\r");

            // checks if daddy g is already happy before doing anything
            if (!body.EndsWith("\r\n"))
            {
                body += "\r\n";
            }

            return body;
        }
        private CacheHandler cache;

        public void init(UserCredential credential, MailAddress mailAddress)
        {
            userCredential = credential;
            this.mailAddress = mailAddress;
            cache = new CacheHandler(mailAddress.Address);

            service = new GmailService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = userCredential,
                ApplicationName = "ExcellentEmailExperience",
            });
        }

        public void setAppClose(CancellationToken appClose)
        {
            this.appClose = appClose;
        }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }

        // this should get all mails 
        public IEnumerable<IMailHandler.Mail> FullSync(string name, int count)
        {
            int returned = 0;
            cache.ClearFolder(name);
            var request = service.Users.Messages.List("me");
            request.LabelIds = name;
            request.MaxResults = 500;
            var messages = request.Execute().Messages;

            List<string> MessageIds = new List<string>();
            List<string> NotCachedMessageIds = new List<string>();

            if (messages == null)
            {
                yield break;
            }

            // Check if the message is in the cache
            foreach (var id in messages.Select(msg => msg.Id))
            {
                MessageIds.Add(id);
                if (!cache.CheckCache(id))
                {
                    NotCachedMessageIds.Add(id);
                }
            }

            for (int i = 0; i < NotCachedMessageIds.Count; i += 100)
            {
                mutex.WaitOne();
                var batchRequest = new BatchRequest(service);
                List<MailContent> mailContents = new List<MailContent>();
                foreach (var id in NotCachedMessageIds.GetRange(i, Math.Min(100, NotCachedMessageIds.Count - i)))
                {
                    var getRequest = service.Users.Messages.Get("me", id);
                    batchRequest.Queue<Google.Apis.Gmail.v1.Data.Message>(getRequest, (content, error, i, message) =>
                    {
                        if (error != null)
                        {
                            Console.WriteLine("Error: " + error.Message);
                            return;
                        }
                        if (content.HistoryId.HasValue)
                        {
                            if (content.HistoryId.Value > LatestId)
                                LatestId = content.HistoryId.Value;
                        }
                        MailContent mailContent = BuildMailContent(content, name);
                        if (!cache.CheckCache(mailContent.MessageId))
                        {
                            cache.CacheMessage(mailContent, name);
                            mailContents.Add(mailContent);
                        }
                    });
                }
                batchRequest.ExecuteAsync(appClose).Wait();
                foreach (var mailContent in mailContents)
                {
                    IMailHandler.Mail newMail = new IMailHandler.Mail();
                    newMail.email = mailContent;
                    newMail.Deletion = false;

                    returned++;
                    if (returned < count)
                    {
                        yield return newMail;
                    }
                }
                //System.Threading.Thread.Sleep(100);
                mutex.ReleaseMutex();
            }

            // Return the messages that are in the cache
            foreach (var message in MessageIds)
            {
                if (cache.CheckCache(message))
                {
                    IMailHandler.Mail newMail = new IMailHandler.Mail();
                    newMail.email = cache.GetMessage(message);
                    newMail.Deletion = false;

                    returned++;
                    if (returned < count)
                    {
                        yield return newMail;
                    }
                }
            }
            UpdateCache();
            if (!fullSyncedFolders.Exists(x => x == name))
                fullSyncedFolders.Add(name);
        }

        // just gets the changes from last sync
        public IEnumerable<IMailHandler.Mail> PartialSync(string folderName)
        {
            // avoid impending doom
            if (NewestId == 0)
            {
                throw new Exception("NewestId not set");
            }

            try
            {
                var request = service.Users.History.List("me");
                request.StartHistoryId = NewestId;
                request.LabelId = folderName;

                var historyResponse = request.Execute();

                LatestId = historyResponse.HistoryId.Value;

                if (historyResponse.History == null)
                {
                    yield break;
                }

                foreach (var history in historyResponse.History)
                {
                    if (history.MessagesAdded != null)
                    {
                        foreach (var message in history.MessagesAdded)
                        {
                            IMailHandler.Mail newMail = new IMailHandler.Mail();
                            if (!cache.CheckCache(message.Message.Id))
                            {
                                message.Message = service.Users.Messages.Get("me", message.Message.Id).Execute();
                                MailContent mail = BuildMailContent(message.Message, folderName);
                                cache.CacheMessage(mail, folderName);

                                newMail.email = mail;
                                newMail.Deletion = false;
                                yield return newMail;
                            }

                            newMail.email = cache.GetMessage(message.Message.Id);
                            newMail.Deletion = false;
                            yield return newMail;
                        }
                    }


                    if (history.MessagesDeleted != null)
                    {
                        foreach (var message in history.MessagesDeleted)
                        {
                            if (cache.CheckCache(message.Message.Id))
                            {
                                MailContent mail = new MailContent();
                                mail.MessageId = message.Message.Id;
                                cache.ClearRow(message.Message.Id);

                                IMailHandler.Mail newMail = new IMailHandler.Mail();
                                newMail.email = mail;
                                newMail.Deletion = true;
                                yield return newMail;
                            }
                        }
                    }

                    if (history.LabelsAdded != null)
                    {
                        foreach (var label in history.LabelsAdded)
                        {
                            if (cache.CheckCache(label.Message.Id))
                            {
                                foreach (var labelId in label.LabelIds)
                                {
                                    if (Label2Flag.ContainsKey(labelId))
                                    {
                                        cache.AddFolder(label.Message.Id, labelId, Label2Flag, "INBOX");
                                        yield return new IMailHandler.Mail
                                        {
                                            email = cache.GetMessage(label.Message.Id),
                                            Deletion = false,
                                            flags = Label2Flag[labelId]
                                        };

                                    }
                                }
                            }
                        }
                    }

                    if (history.LabelsRemoved != null)
                    {
                        foreach (var label in history.LabelsRemoved)
                        {
                            if (cache.CheckCache(label.Message.Id))
                            {
                                foreach (var labelId in label.LabelIds)
                                {
                                    if (Label2Flag.ContainsKey(labelId))
                                    {
                                        cache.RemoveFolder(label.Message.Id, labelId, Label2Flag, "INBOX");

                                        yield return new IMailHandler.Mail
                                        {
                                            email = cache.GetMessage(label.Message.Id),
                                            Deletion = false,
                                            flags = Label2Flag[labelId]
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally { }
        }

        public IEnumerable<IMailHandler.Mail> RefreshOld(string folderName, int count, DateTime time)
        {
            foreach (var OldMail in cache.GetFolder(folderName, count, time))
            {
                IMailHandler.Mail mail = new IMailHandler.Mail();
                mail.email = OldMail;
                yield return mail;
            }
        }

        // this should be called when we want to refresh to view either older or newer messages. 
        public IEnumerable<(string, IMailHandler.Mail)> Refresh(int count)
        {
            bool fullSync = true;
            try
            {
                foreach (var folder in GetFolderNames())
                {

                    foreach (var mail in PartialSync(folder))
                    {
                        yield return (folder, mail);
                    }
                    fullSync = false;
                }

                UpdateCache();

            }
            finally { }

            if (fullSync)
            {
                foreach (var folder in GetFolderNames())
                {
                    foreach (var mail in FullSync(folder, count))
                    {
                        yield return (folder, mail);
                    }
                }
                UpdateCache();
            }
        }

        public void UpdateCache()
        {
            if (LatestId > NewestId)
                NewestId = LatestId;
        }

        // this runs initially. this gets all current mails. caches them, but only displays a certain amount. 
        public IEnumerable<MailContent> GetFolder(string name, int count)
        {
            bool fullSync = true;
            int counter = 0;
            List<MailContent> mailContents = new List<MailContent>();
            try
            {
                foreach (var mail in PartialSync(name))
                {
                    counter++;
                    if (!mail.Deletion)
                    {
                        mailContents.Add(mail.email);
                    }
                }
                fullSync = false;
            }
            catch (Exception)
            {
                Console.WriteLine("Could not partial sync");
            }

            foreach (var mail in mailContents)
            {
                yield return mail;
            }

            if (!fullSyncedFolders.Exists(x => x == name))
                fullSync = true;



            if ((count - counter) > 0)
            {
                bool empty = true;
                foreach (var mail in cache.GetFolder(name, (count - counter)))
                {
                    empty = false;
                    MailContent newMail = new MailContent();
                    newMail = mail;
                    yield return newMail;
                }
                if (empty)
                    fullSync = true;
            }

            if (fullSync)
            {
                foreach (var mail in FullSync(name, count))
                {
                    MailContent NewMail = new MailContent();
                    NewMail = mail.email;
                    yield return NewMail;
                }
            }
        }

        private MailContent BuildMailContent(Google.Apis.Gmail.v1.Data.Message msg, string folderName)
        {
            MailContent mailContent = new();
            mailContent.MessageId = msg.Id;
            mailContent.ThreadId = msg.ThreadId;

            foreach (var label in msg.LabelIds)
            {
                if (Label2Flag.ContainsKey(label))
                {
                    mailContent.flags |= Label2Flag[label];
                }
            }

            if (Label2Flag.ContainsKey(folderName))
            {
                mailContent.flags |= Label2Flag[folderName];
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
                try
                {

                    switch (header.Name)
                    {
                        case "From":
                            mailContent.from = new MailAddress(header.Value);
                            break;
                        case "To":
                            foreach (var address in header.Value.Split(','))
                            {
                                mailContent.to.Add(new MailAddress(address));
                            }
                            break;
                        case "Cc":
                            foreach (var address in header.Value.Split(','))
                            {
                                mailContent.cc.Add(new MailAddress(address));
                            }
                            break;
                        case "Bcc":
                            foreach (var address in header.Value.Split(','))
                            {
                                mailContent.bcc.Add(new MailAddress(address));
                            }
                            break;
                        case "Subject":
                            mailContent.subject = header.Value;
                            break;
                        case "Date":
                            DateTimeOffset date;
                            MimeKit.Utils.DateUtils.TryParse(header.Value, out date);
                            mailContent.date = date.UtcDateTime;
                            break;
                    }
                }
                catch (FormatException e)
                {

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
            else if (messagePart.Body.AttachmentId != null)
            {
                HandleAttachment(messagePart, mailContent);
            }

            if (mailContent.body.EndsWith("\r\n"))
            {
                mailContent.body = mailContent.body.Remove(mailContent.body.Length - 2);
            }
        }

        private void HandleAttachment(MessagePart messagePart, MailContent mailContent)
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

            string extension;
            if (messagePart.Filename == "")
            {
                extension = MimeTypes.MimeTypeMap.GetExtension(messagePart.MimeType);
            }
            else
            {
                extension = "";
            }

            var fileName = messagePart.Filename == "" ? $"Attachment{messagePart.PartId}{extension}" : messagePart.Filename;

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
            labelNames.Remove("UNREAD");
            labelNames.Remove("DRAFT");
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
                    //if (content.MessageId != null)
                    //{
                    //    mimemessage.InReplyTo = content.MessageId;
                    //}
                    //var threads = (service.Users.Threads.Get("me", content.ThreadId).Execute());
                    //if (threads.Messages != null)
                    //{
                    //    List<string> messageIDS = new List<string>();
                    //    foreach (var message in threads.Messages)
                    //    {
                    //        // Add each message ID to the list
                    //        messageIDS.Add(message.Id);
                    //    }
                    //}
                    //    .Users.Threads threadMessageID=service.Users.Threads.Get("me",content.ThreadId);


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
                cache.ClearRow(MessageId);
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
                if (cache.CheckCache(message.Id))
                {
                    yield return cache.GetMessage(message.Id);
                }
                else
                {
                    var msg = service.Users.Messages.Get("me", message.Id).Execute();
                    MailContent mailContent = BuildMailContent(msg, "Search");
                    cache.CacheMessage(mailContent, "Search");
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
            reply.cc.AddRange(content.to);
            reply.from = mailAddress;
            reply.cc.Remove(reply.from);
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

                cache.RemoveFolder(content.MessageId, folder, Label2Flag, "INBOX");
            }
            else
            {
                request.AddLabelIds = new List<string>() { Flag2Label[flagtype] };
                var modifyRequest = service.Users.Messages.Modify(request, "me", content.MessageId);
                modifyRequest.Execute();

                content.flags |= flagtype;

                cache.AddFolder(content.MessageId, folder, Label2Flag, "INBOX");
            }

            return content;
        }
    }
}

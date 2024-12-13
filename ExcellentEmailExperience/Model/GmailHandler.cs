using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Xml.Linq;
using Windows.Storage;
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

        public List<MailAddress> flaggedMails { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private UserCredential userCredential;
        private GmailService service;
        private MailAddress mailAddress;
        public ulong NewestId;
        public ulong LastId;

        // modifies the body string so that google doesn't shit itself in fear and panic
        // and therefore modifies the message to fit its asinine standards
        public string MakeDaddyGHappy(string body)
        {
            body = body.Replace("\n", "\r\n");
            body = body.Replace(" \r", "\r");
            body += "\r\n";

            return body;
        }
        private CacheHandler cache;

        public GmailHandler(UserCredential credential, MailAddress mailAddress)
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


        public ulong[] GetNewIds()
        {
            var request = service.Users.Messages.List("me");

            request.MaxResults = 20;
            ulong[] Ids = new ulong[2];
            Ids[0] = NewestId;
            Ids[1] = LastId;
            var refreq = service.Users.History.List("me");
            refreq.StartHistoryId = NewestId;
            var historyResponse = refreq.Execute();
            // Update the newest history ID after processing the refresh
            if (historyResponse.HistoryId.HasValue)
            {
                NewestId = historyResponse.HistoryId.Value;
            }

            var refreqOld = service.Users.Messages.List("me");
            refreqOld.MaxResults = 20;

            refreqOld.Q = $"before:{LastId}";

            var messageListResponse = refreqOld.Execute();
            if (messageListResponse.Messages == null || messageListResponse.Messages.Count == 0)
            {
                Ids[1] = LastId;
                return Ids;
            }

            var NewLastMessage = service.Users.Messages.Get("me", messageListResponse.Messages[^1].Id).Execute();
            LastId = NewLastMessage.HistoryId.Value;

            Ids[0] = NewestId;
            Ids[1] = LastId;
            return Ids;

        }

        public bool CheckSpam(MailContent content)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<MailContent> Refresh(string name, bool old, int count, ulong lastId, ulong newestId)
        {
            if (!old)
            {
                var refreq = service.Users.History.List("me");
                refreq.StartHistoryId = newestId;
                refreq.LabelId = name;
                var historyResponse = refreq.Execute();
                // Update the newest history ID after processing the refresh
                //if (historyResponse.HistoryId.HasValue)
                //{
                //    newestId = historyResponse.HistoryId.Value;
                //}

                //if (historyResponse.NextPageToken == null)
                //{
                //    yield break;
                //}
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
                    foreach (var addedMessage in history.MessagesAdded)
                    {
                        if (cache.CheckCache(addedMessage.Message.Id))
                        {
                            yield return cache.GetCache(addedMessage.Message.Id);
                        }
                        else
                        {
                            var msg = service.Users.Messages.Get("me", addedMessage.Message.Id).Execute();

                            // im just going to assume that when you get a new mail that hasnt been seen before that its also unread. 
                            MailContent mailContent = BuildMailContent(msg, msg.LabelIds[0]);
                            foreach (string foldername in addedMessage.Message.LabelIds)
                            {
                                cache.CacheMessage(mailContent, foldername);
                            }
                            yield return mailContent;
                        }
                    }
                }
                yield break;
            }
            else
            {
                var refreqOld = service.Users.Messages.List("me");
                refreqOld.MaxResults = count;

                refreqOld.Q = $"before:{lastId}";

                var messageListResponse = refreqOld.Execute();
                if (messageListResponse.Messages == null || messageListResponse.Messages.Count == 0)
                {
                    yield break;
                }

                foreach (var message in messageListResponse.Messages)
                {
                    if (cache.CheckCache(message.Id))
                    {
                        yield return cache.GetCache(message.Id);
                    }
                    else
                    {
                        var msg = service.Users.Messages.Get("me", message.Id).Execute();

                        // this probably is not the correct label but i cant for the life of me figure out how to pass the labes in correctly here. 
                        MailContent mailContent = BuildMailContent(msg, msg.LabelIds[0]);
                        foreach (string foldername in msg.LabelIds)
                        {
                            cache.CacheMessage(mailContent, foldername);
                        }
                        yield return mailContent;
                    }
                }

                //var NewLastMessage = service.Users.Messages.Get("me", messageListResponse.Messages[^1].Id).Execute();
                //LastId = NewLastMessage.HistoryId.Value;
            }
        }

        public IEnumerable<MailContent> GetFolder(string name, int count)
        {

            var request = service.Users.Messages.List("me");
            request.LabelIds = name;
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
                    cache.AddFolders(message.Id, name, Label2Flag, "INBOX");
                    yield return cache.GetCache(message.Id);
                }
                else
                {
                    var msg = service.Users.Messages.Get("me", message.Id).Execute();
                    MailContent mailContent = BuildMailContent(msg, name);
                    cache.CacheMessage(mailContent, name);
                    yield return mailContent;
                }
            }
            var NewestMessage = service.Users.Messages.Get("me", messages[0].Id).Execute();
            NewestId = NewestMessage.HistoryId.Value;

            var LastMessage = service.Users.Messages.Get("me", messages[^1].Id).Execute();
            LastId = LastMessage.HistoryId.Value;

            yield break;
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
                    yield return cache.GetCache(message.Id);
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
            Mail.body = $"Forwarded from {content.from}\n {content.body} \n\n Originally sent to:{content.to}";

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

                cache.RemoveFolders(content.MessageId, folder, Label2Flag, "INBOX");
            }
            else
            {
                request.AddLabelIds = new List<string>() { Flag2Label[flagtype] };
                var modifyRequest = service.Users.Messages.Modify(request, "me", content.MessageId);
                modifyRequest.Execute();

                content.flags |= flagtype;

                cache.AddFolders(content.MessageId, folder, Label2Flag, "INBOX");
            }

            return content;
        }
    }
}

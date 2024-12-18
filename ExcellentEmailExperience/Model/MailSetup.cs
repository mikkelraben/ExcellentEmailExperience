using MimeKit;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace ExcellentEmailExperience.Model
{
    internal class MailSetup
    {
        static public MimeMessage SetupMail(MailContent content)
        {
            if (content.to.Count == 0)
            {
                MessageHandler.AddMessage("Cannot send mail to no one, try adding an email in the To field", MessageSeverity.Error);
                throw new ArgumentException("A receiver of null was inappropriately allowed.");
            }
            if (content.subject == "")
            {
                MessageHandler.AddMessage("Cannot send mail with no subject, try adding a subject", MessageSeverity.Error);
                throw new ArgumentException("A subject of null was inappropriately allowed.");
            }

            content.date = DateTime.Now;
            // creates a new mailmessage object, these are the ones that we need to setup before sending
            var message = new MailMessage
            {
                Subject = content.subject,
                From = content.from
            };

            // adds bcc,cc,and recipient.
            try
            {
                foreach (var recipient in content.to)
                {
                    message.To.Add(recipient);
                }
            }
            catch
            {

            }

            //TODO: if these fields are empty, the program will crash. please rethrow the exceptions below

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

            try
            {
                foreach (var attachment in content.attachments)
                {
                    if (!File.Exists(attachment)) // does the file exist
                    {
                        throw new FileNotFoundException("attachment not found", attachment);
                    }
                    Debug.WriteLine("File Exists");
                    string Type = MimeKit.MimeTypes.GetMimeType(attachment);// defines what type of attachment it is
                    Debug.WriteLine("type is:" + Type);
                    Attachment Attachment = new Attachment(attachment); // attach it
                    Attachment.ContentType = new System.Net.Mime.ContentType(Type); // parse with correct type
                    message.Attachments.Add(Attachment); // brrrrrrrrrrrr
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error in attachments" + ex);
            }
            
            
            //after creating the maintext we need to add it to the mailmessage object
            message.AlternateViews.Add(MessageContent);
            // convert to mimemessage, this is necessary for sending
            return MimeMessage.CreateFromMailMessage(message);

        }
    }
}

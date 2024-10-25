using ExcellentEmailExperience.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public class GmailSender : ISender
    {
        /*
        this class only does one thing. it takes a content Class and sends the data in the class as an email 
        this one is specific to GMAIL. and should only be used by the GMAILHandler. 
         */
        public void SendMail(MailContent Content)
        {

            using (var gmailService = new GmailService(new BaseClientService.Initializer() { HttpClientInitializer = this._userCredential }))
            {
                var profileRequest = gmailService.Users.GetProfile("me");
                var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
                var message = new MailMessage
                {
                    Subject = Content.subject,
                    From = new MailAddress(user.EmailAddress)
                };
                foreach (var recipient in Content.to)
                {
                    message.To.Add(new MailAddress(recipient));
                }

                var MessageContent = AlternateView.CreateAlternateViewFromString(Content.body, new System.Net.Mime.ContentType("text/plain"));
                MessageContent.ContentType.CharSet = Encoding.UTF8.WebName;
                message.AlternateViews.Add(MessageContent);

                var mimemessage = MimeMessage.CreateFromMailMessage(message);

                // this part is made with cocreation along side chat-GPT 
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

                    var sendRequest = gmailService.Users.Messages.Send(gmailMessage, "me");
                    sendRequest.Execute();
                }
            }
        }


    }
    }
}

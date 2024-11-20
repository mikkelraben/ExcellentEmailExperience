using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using System;
using System.Net.Mail;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExcellentEmailExperience.Model
{
    public class GmailAccount : IAccount
    {
        private UserCredential userCredential;

        [JsonInclude]
        public string accountName;
        [JsonInclude]
        public MailAddress mailAddress;


        // Yes this is relatively safe, as this only allows an application permission to read and send emails. Any one can create these credentials and use them to access their own emails.
        // If this needed to be more secure then encryption would be needed, though this would require a user to enter a password every time they wanted to use the application.
        readonly string clientID = "707664940798-98kh872lnb9t4pjd4srieahk6duq4sh0.apps.googleusercontent.com";
        readonly string clientSecret = "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";


        GmailHandler handler;

        public IMailHandler GetMailHandler()
        {
            return handler;
        }

        public void Login(string email, string secret = "")
        {
            ClientSecrets clientSecrets = new()
            {
                ClientId = clientID,
                ClientSecret = clientSecret
            };

            CredentialHandlerGoogleShim credentialHandlerGoogleShim = new();
            if (email == null)
            {
                userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    ["https://mail.google.com/"],
                    "This account name cannot be used",
                    System.Threading.CancellationToken.None, credentialHandlerGoogleShim).Result;

                CredentialHandler.RemoveCredential("This account name cannot be used");

                GmailService service = new GmailService(new Google.Apis.Services.BaseClientService.Initializer()
                {
                    HttpClientInitializer = userCredential,
                    ApplicationName = "ExcellentEmailExperience",
                });

                var profileRequest = service.Users.GetProfile("me");
                var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
                mailAddress = new MailAddress(user.EmailAddress);

                CredentialHandler.AddCredential(mailAddress.Address, JsonSerializer.Serialize(userCredential.Token));
            }
            else
            {
                userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    clientSecrets,
                    ["https://mail.google.com/"],
                    email,
                    System.Threading.CancellationToken.None, credentialHandlerGoogleShim).Result;
            }

            if (userCredential == null)
            {
                throw new Exception("Login failed");
            }
            handler = new GmailHandler(userCredential, mailAddress);
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public bool TryLogin(string username, string secret)
        {
            ClientSecrets clientSecrets = new()
            {
                ClientId = clientID,
                ClientSecret = clientSecret
            };

            CredentialHandlerGoogleShim credentialHandlerGoogleShim = new();

            userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                ["https://mail.google.com/"],
                username,
                System.Threading.CancellationToken.None, credentialHandlerGoogleShim).Result;

            if (userCredential != null)
            {
                handler = new GmailHandler(userCredential, mailAddress);
            }

            return userCredential != null;
        }

        public string GetName()
        {
            return accountName;
        }

        public void SetName(string name)
        {
            accountName = name;
        }

        public MailAddress GetEmail()
        {
            return mailAddress;
        }
    }
}

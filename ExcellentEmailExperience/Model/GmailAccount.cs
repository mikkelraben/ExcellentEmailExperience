using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using System;

namespace ExcellentEmailExperience.Model
{
    public class GmailAccount : IAccount
    {
        private UserCredential userCredential;
        private string accountName;


        // Yes this is relatively safe, as this only allows an application permission to read and send emails. Any one can create these credentials and use them to access their own emails.
        // If this needed to be more secure then encryption would be needed, though this would require a user to enter a password every time they wanted to use the application.
        readonly string clientID = "707664940798-98kh872lnb9t4pjd4srieahk6duq4sh0.apps.googleusercontent.com";
        readonly string clientSecret = "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";



        public IMailHandler GetMailHandler()
        {
            return new GmailHandler(userCredential);
        }

        public void Login(string username)
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


            //TODO: Remove this line since the account name is set on setup or in settings, but this is not implemented yet.
            accountName = username;
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
    }
}

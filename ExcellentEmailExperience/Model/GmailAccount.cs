using ExcellentEmailExperience.Interfaces;
using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public class GmailAccount : IAccount
    {
        private UserCredential userCredential;

        public IMailHandler GetMailHandler()
        {
            return new GmailHandler(userCredential);
        }

        public void Login()
        {
            // Yes this is relatively safe, as this only allows an application permission to read and send emails. Any one can create these credentials and use them to access their own emails.
            // If this needed to be more secure then encryption would be needed, though this would require a user to enter a password every time they wanted to use the application.
            const string clientID = "707664940798-98kh872lnb9t4pjd4srieahk6duq4sh0.apps.googleusercontent.com";
            const string clientSecret = "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";


            ClientSecrets clientSecrets = new()
            {
                ClientId = clientID,
                ClientSecret = clientSecret
            };



            Google.Apis.Util.Store.FileDataStore fileDataStore = new("Google.Apis.Auth");

            fileDataStore.GetAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>("user").Wait();

            userCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                ["https://mail.google.com/"],
                "user",
                System.Threading.CancellationToken.None).Result;
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }
    }
}

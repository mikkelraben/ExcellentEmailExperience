using ExcellentEmailExperience.Views;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.AppContainer;
using System;

using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model; //including mail functionalities from our project
using System.Net.Mail; //including mail functionalities from .NET
using System.Collections.Generic;
using System.Linq;
using Org.BouncyCastle.Crypto.Macs;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Assert.AreEqual(2, 2);

            MailAddress validAddress = new MailAddress("bias@gmail.com","bias");
            MailAddress validAddress2 = new MailAddress("nobias@gmail.com");
            MailAddress validAddress3 = new MailAddress("thebias@gmail.com");
            string validSubject = "Hello";
            string validBody = "Hello, how are you?";
            string validAttachment = "C:/Users/Downloads"; //valid attachment path maybe
            string invalidAttachment = "C:/Users/Downloads"; //invalid attachment path maybe
            string username1 = "lillekatemil6@gmail.com";
            string username2 = "postmanpergruppe1@gmail.com";

            //instanciating an account with IAccount object
            IAccount account1 = new GmailAccount();
            IAccount account2 = new GmailAccount();

            

            //accessing the refresh tokens from the environment variables in github
            string? REFRESHTOKEN1 = Environment.GetEnvironmentVariable("REFRESHTOKEN1");

            string? REFRESHTOKEN2 = Environment.GetEnvironmentVariable("REFRESHTOKEN2");

            //creating a token response object with the refresh token
            Google.Apis.Auth.OAuth2.Responses.TokenResponse tokenResponse1 = new Google.Apis.Auth.OAuth2.Responses.TokenResponse();
            tokenResponse1.RefreshToken = REFRESHTOKEN1;

            Google.Apis.Auth.OAuth2.Responses.TokenResponse tokenResponse2 = new Google.Apis.Auth.OAuth2.Responses.TokenResponse();
            tokenResponse2.RefreshToken = REFRESHTOKEN2;

            //creating a file data store object and storing the token response in it (seperate for the accounts)
            Google.Apis.Util.Store.FileDataStore fileDataStore = new("Google.Apis.Auth");


            fileDataStore.StoreAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>(username1,tokenResponse1);

            fileDataStore.StoreAsync<Google.Apis.Auth.OAuth2.Responses.TokenResponse>(username2,tokenResponse2);


            //logging in to the account
            account1.Login(username1);
            account2.Login(username2);


            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();

            //accessing the password from the environment variables in github?

            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail =mailHandler1.NewMail(validAddress,validSubject,validAddress3,validAddress2,validBody,null);
            
            int inboxLength = mailHandler.GetInbox().Length;

            mailHandler.Send(validMail);

            Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength+1);

            //checking recieved mail for spam mail
            Assert.IsFalse(mailHandler.CheckSpam(mailHandler.GetInbox()[0]));

            //forwarding the recieved mail to the current mail address?
            mailHandler.Forward(mailHandler.GetInbox()[inboxLength+1]);

            Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 2);

            //testing that the recieved mail and the forwarded mail are the same
            Assert.IsTrue(mailHandler.GetInbox()[inboxLength + 1] == mailHandler.GetInbox()[inboxLength + 1]);

            //replying to the recieved mail and checking the reply is recieved
            mailHandler.Reply(mailHandler.GetInbox()[inboxLength + 1]);

            Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 3);



            //replying to all the recieved mail and checking the reply is recieved
            mailHandler.ReplyAll(mailHandler.GetInbox()[inboxLength + 1]);

            Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 4);

        }
    }
}

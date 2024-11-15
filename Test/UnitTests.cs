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
        
        //creating a new mail address object

        MailAddress Address1 = new MailAddress("lillekatemil6@gmail.com", "bias");
        MailAddress Address2 = new MailAddress("postmanpergruppe1@gmail.com");
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
        //accessing the refresh tokens from the environment variables stored locally on pc
        string? REFRESHTOKEN1 = Environment.GetEnvironmentVariable("REFRESHTOKEN1");

        string? REFRESHTOKEN2 = Environment.GetEnvironmentVariable("REFRESHTOKEN2");
        
        public UnitTest1()
        {
           
            CredentialHandler.AddCredential(username1, REFRESHTOKEN1);

            CredentialHandler.AddCredential(username2, REFRESHTOKEN2);


            //logging in to the account
            account1.Login(username1);
            account2.Login(username2);
        }


        [TestMethod]
        public void TestMethod_login()
        {
            Assert.IsTrue(account1.TryLogin(username1,REFRESHTOKEN1));

        }



        [TestMethod]
        public void TestMethod_send()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();


            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = mailHandler1.NewMail(Address2, validSubject, null, null, null, null);

            mailHandler1.Send(validMail);

            //TODO: change amount of requests when api is changed


            //folder is index in ["Inbox", "Sent", "Drafts", "Spam", "Trash"];
            List<MailContent> Inboxlist2 =GetInbox(mailHandler2, 0);


            List<MailContent> Sentlist1 =GetInbox(mailHandler1, 1);

            Inboxlist2[0].date - Sentlist1[0].date;

            Assert.IsTrue( == );

            ////checking recieved mail for spam mail
            //Assert.IsFalse(mailHandler.CheckSpam(mailHandler.GetInbox()[0]));

            ////forwarding the recieved mail to the current mail address?
            //mailHandler.Forward(mailHandler.GetInbox()[inboxLength+1]);

            //Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 2);

            ////testing that the recieved mail and the forwarded mail are the same
            //Assert.IsTrue(mailHandler.GetInbox()[inboxLength + 1] == mailHandler.GetInbox()[inboxLength + 1]);

            ////replying to the recieved mail and checking the reply is recieved
            //mailHandler.Reply(mailHandler.GetInbox()[inboxLength + 1]);

            //Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 3);



            ////replying to all the recieved mail and checking the reply is recieved
            //mailHandler.ReplyAll(mailHandler.GetInbox()[inboxLength + 1]);

            //Assert.IsTrue(mailHandler.GetInbox().Length == inboxLength + 4);

        }

        private static List<MailContent> GetInbox(IMailHandler mailHandler,int folder)
        {

            //TODO: change amount of requests when api is changed

            string[] folderNames;
            List<MailContent> inbox = new(); 

            //returns string with ["Inbox", "Sent", "Drafts", "Spam", "Trash"];
            folderNames = mailHandler.GetFolderNames();
            


            foreach (var mail in mailHandler.GetFolder(folderNames[folder], false, false))
            {
                inbox.Add(mail);
                inbox.Sort((x, y) => DateTime.Parse(x.date).CompareTo(DateTime.Parse(y.date)));
            };
            return inbox;
        }
    }
}

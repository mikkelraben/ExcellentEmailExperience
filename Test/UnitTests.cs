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
        MailAddress Address3 = new MailAddress("postmandpersbil@gmail.com"); //PENDING new real account
        string validSubject = "For kitty";
        string validAttachment = "C:/Users/Downloads"; //valid attachment path maybe
        string invalidAttachment = "C:/Users/Downloads"; //invalid attachment path maybe
        string username1 = "lillekatemil6@gmail.com";
        string username2 = "postmanpergruppe1@gmail.com";
        string username3 = "postmandpersbil@gmail.com"; //PENDING new real account

        //instanciating an account with IAccount object
        IAccount account1 = new GmailAccount();
        IAccount account2 = new GmailAccount();
        IAccount account3 = new GmailAccount(); //PENDING new real account

        //accessing the refresh tokens from the environment variables stored locally on pc
        string? REFRESHTOKEN1 = Environment.GetEnvironmentVariable("REFRESHTOKEN1");

        string? REFRESHTOKEN2 = Environment.GetEnvironmentVariable("REFRESHTOKEN2");

        string? REFRESHTOKEN3 = Environment.GetEnvironmentVariable("REFRESHTOKEN3"); //PENDING new real account

        string? validBody;

        private string validBody_gen()
        {
            int randomInteger = new Random().Next(0, 1000);
            validBody = string.Format("Hello sweetpeach, i have a new buiscuit waiting for you \n love grandma {0}", randomInteger);
            return validBody;
        }

        private static List<MailContent> GetInbox(IMailHandler mailHandler, string folder)
        {

            //TODO: change amount of requests when api is changed

            List<MailContent> inbox = new();


            foreach (var mail in mailHandler.GetFolder(folder, false, false))
            {
                inbox.Add(mail);
                inbox.Sort((x, y) => -x.date.CompareTo(y.date));
            };
            return inbox;
        }

        [TestInitialize]
        public void UnitTest_init()
        {


            CredentialHandler.AddCredential(username1, REFRESHTOKEN1);

            CredentialHandler.AddCredential(username2, REFRESHTOKEN2);

            //CredentialHandler.AddCredential(username3, REFRESHTOKEN3); //PENDING new real account


            //logging in to the account
            account1.Login(username1);
            account2.Login(username2);
            //account3.Login(username3); //PENDING new real account
        }


        [TestMethod]
        public void TestMethod_login()
        {
            Assert.IsTrue(account1.TryLogin(username1, REFRESHTOKEN1));

        }



        [TestMethod]
        public void TestMethod_send_speed()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();


            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.body = validBody_gen();

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            mailHandler1.Send(validMail);

            //TODO: change amount of requests when api is changed


            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            if (Inboxlist2[0] == null && Sentlist1[0] == null)
            {
                Assert.Fail("no messages were sent!");
            }

            //checking if the time difference between the sent mail and recieved mail is less than 1 second??

            TimeSpan diff = Inboxlist2[0].date.Subtract(Sentlist1[0].date);


            Assert.IsTrue(diff.TotalSeconds < 1 && diff.TotalSeconds > -1);

        }



        [TestMethod]
        public void TestMethod_send_content()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();
            IMailHandler mailHandler3 = account3.GetMailHandler();


            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address1 };


            validMail.bcc = new List<MailAddress> { Address3 };




            mailHandler1.Send(validMail);

            //TODO: change amount of requests when api is changed


            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");


            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist2[0] != null && Sentlist1[0] != null)
            {
                Inboxlist2[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist2[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Inboxlist2[0] == Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "A receiver of null was inappropriately allowed.")]
        public void TestMethod_invalid_receiver()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent UnvalidMail = new();

            UnvalidMail.subject = validSubject;

            UnvalidMail.from = Address1;
            mailHandler1.Send(UnvalidMail);


        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "A subject of null was inappropriately allowed.")]
        public void TestMethod_invalid_subject()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent UnvalidMail = new();

            UnvalidMail.to = new List<MailAddress> { Address2 };

            UnvalidMail.from = Address1;
            mailHandler1.Send(UnvalidMail);


        }

        [TestMethod]
        public void TestMethod_cc()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();
            IMailHandler mailHandler3 = account3.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");


            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist3[0] != null && Sentlist1[0] != null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Inboxlist3[0] == Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        public void TestMethod_bcc()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();
            IMailHandler mailHandler3 = account3.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.bcc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist3[0] != null && Sentlist1[0] != null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Inboxlist3[0] == Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        public void TestMethod_reply()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();
            IMailHandler mailHandler3 = account3.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.bcc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");
            
        }

        [TestMethod]
        public void TestMethod_forward()
        {

            //instantiating a GmailHandler object
            IMailHandler mailHandler1 = account1.GetMailHandler();
            IMailHandler mailHandler2 = account2.GetMailHandler();
            IMailHandler mailHandler3 = account3.GetMailHandler();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            mailHandler1.Send(validMail);



            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            if(Inboxlist2[0] != null)
            {
                mailHandler2.Forward(Inboxlist2[0], new List<MailAddress> { Address3 });
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Sentlist1[0] != null && Inboxlist3[0]!=null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Inboxlist3[0] == Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }
    }
}

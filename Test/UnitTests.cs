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
        string validAttachment = @"~\..\..\..\..\..\..\ExcellentEmailExperience\Assets\Icon.png"; //valid attachment path maybe testBranch
        string invalidAttachment = "C:/Users/Downloads"; //invalid attachment path maybe
        string username1 = "lillekatemil6@gmail.com";
        string username2 = "postmanpergruppe1@gmail.com";
        string username3 = "postmandpersbil@gmail.com"; //PENDING new real account 

        //instanciating an account with IAccount object
        IAccount account1 = new GmailAccount();
        IAccount account2 = new GmailAccount();
        IAccount account3 = new GmailAccount(); //PENDING new real account
        MailApp MailApp = new MailApp();
        //accessing the refresh tokens from the environment variables stored locally on pc
        string? REFRESHTOKEN1 = Environment.GetEnvironmentVariable("REFRESHTOKEN1");

        string? REFRESHTOKEN2 = Environment.GetEnvironmentVariable("REFRESHTOKEN2");

        string? REFRESHTOKEN3 = Environment.GetEnvironmentVariable("REFRESHTOKEN3"); //PENDING new real account

        string? validBody;

        IMailHandler? mailHandler1;
        IMailHandler? mailHandler2;
        IMailHandler? mailHandler3;

        private string validBody_gen()
        {
            int randomInteger = new Random().Next(0, 1000);
            validBody = string.Format("Hello sweetpeach, i have a new buiscuit waiting for you\r\n love grandma {0}", randomInteger);
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

            MailApp.NewAccount(account1);

            CredentialHandler.AddCredential(username1, REFRESHTOKEN1);

            CredentialHandler.AddCredential(username2, REFRESHTOKEN2);

            CredentialHandler.AddCredential(username3, REFRESHTOKEN3); //PENDING new real account


            //logging in to the account
            account1.Login(username1);
            account2.Login(username2);
            account3.Login(username3); //PENDING new real account


            //instantiating a GmailHandler object
            mailHandler1 = account1.GetMailHandler();
            mailHandler2 = account2.GetMailHandler();
            mailHandler3 = account3.GetMailHandler();
        }


        [TestMethod]
        public void TestMethod_login()
        {
            Assert.IsTrue(account1.TryLogin(username1, REFRESHTOKEN1));

        }



        [TestMethod]
        public void TestMethod_send_speed()
        {



            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.body = validBody_gen();

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            mailHandler1.Send(validMail);

            //TODO: change amount of requests when api is changed

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            if (Inboxlist2[0] == null && Sentlist1[0] == null)
            {
                Assert.Fail("no messages were sent!");
            }

            //checking if the time difference between the sent mail and recieved mail is less than 2 second??

            TimeSpan diff = Inboxlist2[0].date.Subtract(Sentlist1[0].date);


            Assert.IsTrue(diff.TotalSeconds < 1 && diff.TotalSeconds > -1);

        }



        [TestMethod]
        public void TestMethod_send_content()
        {



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

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

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

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent UnvalidMail = new();

            UnvalidMail.to = new List<MailAddress> { Address2 };

            UnvalidMail.from = Address1;
            mailHandler1.Send(UnvalidMail);


        }

        [TestMethod]
        public void TestMethod_cc()
        {


            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

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
            

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.bcc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

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


            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            string response = validBody_gen();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            if (Inboxlist3[0] != null)
            {
                mailHandler3.Reply(Inboxlist3[0], response);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist1 = GetInbox(mailHandler1, "INBOX");

            List<MailContent> Sentlist3 = GetInbox(mailHandler3, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist1[0] != null && Sentlist3[0] != null)
            {
                Sentlist3[0].MessageId = Inboxlist1[0].MessageId;
                Sentlist3[0].ThreadId = Inboxlist1[0].ThreadId;

                //here we also have to set cc to null as reply doesnt include cc
                Sentlist3[0].cc = null;
                Sentlist3[0].subject = "RE: " + validSubject;

                Assert.IsTrue(Sentlist3[0] == Inboxlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

        }

        [TestMethod]
        public void TestMethod_reply_all()
        {


            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            string response = validBody_gen();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            if (Inboxlist3[0] != null)
            {
                mailHandler3.ReplyAll(Inboxlist3[0], response);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            List<MailContent> Sentlist3 = GetInbox(mailHandler3, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Sentlist1[0] != null && Sentlist3[0] != null)
            {
                Sentlist3[0].MessageId = Sentlist1[0].MessageId;
                Sentlist3[0].ThreadId = Sentlist1[0].ThreadId;
                Sentlist3[0].subject = "RE: " + validSubject;

                Assert.IsTrue(Sentlist3[0] == Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }
        }

        [TestMethod]
        public void TestMethod_forward()
        {

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            if(Inboxlist2[0] != null)
            {
                mailHandler2.Forward(Inboxlist2[0], new List<MailAddress> { Address3 });
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

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

        [TestMethod]
        public void TestMethod_attachment()
        {

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.attachments = new List<string> { validAttachment };

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(4000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist2[0] != null && Sentlist1[0] != null)
            {
                Inboxlist2[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist2[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.AreEqual(Inboxlist2[0], Sentlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }
        }

        [TestMethod]
        public void TestMethod_trash()
        {

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");
            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            if (Inboxlist2[0] != null)
            {
                mailHandler2.TrashMail(Inboxlist2[0].MessageId);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(2000);

            List<MailContent> Trashlist2 = GetInbox(mailHandler2, "TRASH");

            Assert.IsTrue(Trashlist2[0] == Sentlist1[0]);
            Assert.IsTrue(Trashlist2[0] != Inboxlist2[0]);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "Cant add account twice.")]
        public void TestMethod_Multiple_acc()
        {
            MailApp.NewAccount(account2);

            MailApp.NewAccount(account1);
        }

        [TestMethod]
        public void TestMethod_DeleteAccount()
        {
            MailApp MailApp1 = new();
            MailApp1.NewAccount(account1);
            MailApp1.DeleteAccount(account1);
            Assert.IsFalse(MailApp1.HasAccount());

        }
    }
}

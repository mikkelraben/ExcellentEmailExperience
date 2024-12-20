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
using System.Threading;
using System.Diagnostics;
using System.Text;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {

        //creating a new mail address object

        MailAddress Address1_ = new MailAddress("lillekatemil6@gmail.com", "bias");
        MailAddress Address2_ = new MailAddress("postmanpergruppe1@gmail.com");
        MailAddress Address3_ = new MailAddress("postmandpersbil@gmail.com"); //PENDING new real account
        MailAddress Address1;
        MailAddress Address2;
        MailAddress Address3;

        private static Mutex mut = new();

        string validSubject = "For kitty";
        string validAttachment = @"~\..\..\..\..\..\..\ExcellentEmailExperience\Assets\Icon.png"; //valid attachment path maybe testBranch
        string validAttachment1 = @"~\..\..\..\..\..\..\ExcellentEmailExperience\Assets\TextFile1.txt"; //valid attachment path maybe testBranch
        string invalidAttachment = "C:/Users/Downloads"; //invalid attachment path maybe
        string username1 = "lillekatemil6@gmail.com";
        string username2 = "postmanpergruppe1@gmail.com";
        string username3 = "postmandpersbil@gmail.com"; //PENDING new real account 

        //instanciating an account with IAccount object
        IAccount account1 = new GmailAccount();
        IAccount account2 = new GmailAccount();
        IAccount account3 = new GmailAccount(); //PENDING new real account
        MailApp MailApp = new MailApp();
        string validwhitespacebody = "\n\r\r\r\n         \r\r\n    m";
        //accessing the refresh tokens from the environment variables stored locally on pc
        string? REFRESHTOKEN1 = Environment.GetEnvironmentVariable("REFRESHTOKEN1");
        string? REFRESHTOKEN2 = Environment.GetEnvironmentVariable("REFRESHTOKEN2");

        string? REFRESHTOKEN3 = Environment.GetEnvironmentVariable("REFRESHTOKEN3"); //PENDING new real account

        string? validBody;

        IMailHandler? mailHandler1;
        IMailHandler? mailHandler2;
        IMailHandler? mailHandler3;

        public string MakeDaddyGHappy(string body)
        {
            body = body.Replace("\n", "\r\n");
            body = body.Replace(" \r", "\r");
            body += "\r\n";

            return body;
        }

        private void UnitTest_init()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            List<MailAddress> mailAddresses = new List<MailAddress> { Address1_, Address2_, Address3_ };
            List<string> Refreshtokens = new List<string> { REFRESHTOKEN1, REFRESHTOKEN2, REFRESHTOKEN3 };
            List<string> usernames = new List<string> { username1, username2, username3 };
            //create a list with the numbers 1,2,3 and shuffle them:


            List<int> IntegerList = IntRemix();
            Address1 = mailAddresses[IntegerList[0]];
            Address2 = mailAddresses[IntegerList[1]];
            Address3 = mailAddresses[IntegerList[2]];


            MailApp.NewAccount(account1);

            CredentialHandler.AddCredential(usernames[IntegerList[0]], Refreshtokens[IntegerList[0]]);

            CredentialHandler.AddCredential(usernames[IntegerList[1]], Refreshtokens[IntegerList[1]]);

            CredentialHandler.AddCredential(usernames[IntegerList[2]], Refreshtokens[IntegerList[2]]);

            //logging in to the account

            account1.Login(usernames[IntegerList[0]]);
            account2.Login(usernames[IntegerList[1]]);
            account3.Login(usernames[IntegerList[2]]);


            //instantiating a GmailHandler object
            mailHandler1 = account1.GetMailHandler();
            mailHandler2 = account2.GetMailHandler();
            mailHandler3 = account3.GetMailHandler();
        }



        private string validBody_gen()
        {
            int randomInteger = new Random().Next(0, 1000);
            validBody = string.Format("Hello sweetpeach, i have a new buiscuit waiting for you \n love grandma {0}", randomInteger);
            return validBody;
        }


        private List<int> IntRemix()
        {
            Random rng = new Random();

            List<int> list = new List<int> { 0, 1, 2 };
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }


        private static List<MailContent> GetInbox(IMailHandler mailHandler, string folder)
        {

            //TODO: change amount of requests when api is changed

            List<MailContent> inbox = new();


            foreach (var mail in mailHandler.GetFolder(folder, 20))
            {
                inbox.Add(mail);
                inbox.Sort((x, y) => -x.date.CompareTo(y.date));
            };
            return inbox;
        }




        [TestMethod]
        public void TestMethod_login()
        {
            UnitTest_init();
            Assert.IsTrue(account1.TryLogin(username1, REFRESHTOKEN1));

        }


        [TestMethod]
        public void TestMethod_send_content()
        {
            UnitTest_init();
            //creating a new mail object and sending it to another mail address. 
            MailContent validMail = new();

            validMail.subject = validSubject;
            validMail.to = new List<MailAddress> { Address2 };
            validMail.from = Address1;
            validMail.body = validBody_gen();
            validMail.cc = new List<MailAddress> { Address1 };
            validMail.bcc = new List<MailAddress> { Address3 };


            mut.WaitOne(); Debug.WriteLine("getting mutex access");
            mailHandler1.Send(validMail);
            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");
            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects


            if (Inboxlist2[0] != null && Sentlist1[0] != null)
            {
                //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects
                //the threadID would be different for mails sent forth and back, so we dont need to compare them
                Inboxlist2[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist2[0].ThreadId = Sentlist1[0].ThreadId;
                Assert.IsTrue(Inboxlist2[0].body == Sentlist1[0].body);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "A receiver of null was inappropriately allowed.")] //expects an exception
        public void TestMethod_invalid_receiver()
        {
            UnitTest_init();

            // creating a new mail object without filling the "to" field and sending it to the current mail address. 
            MailContent UnvalidMail = new();

            UnvalidMail.subject = validSubject;

            UnvalidMail.from = Address1;

            mut.WaitOne(); Debug.WriteLine("getting mutex access");
            try
            {
                //due to exception we try..catch to release the mutex
                mailHandler1.Send(UnvalidMail);
            }
            finally
            {
                Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();


            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException),
        "A subject of null was inappropriately allowed.")]
        public void TestMethod_invalid_subject()
        {
            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent UnvalidMail = new();

            UnvalidMail.to = new List<MailAddress> { Address2 };

            UnvalidMail.from = Address1;
            mut.WaitOne(); Debug.WriteLine("getting mutex access");
            try
            {

                mailHandler1.Send(UnvalidMail);
            }
            finally
            {
                Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            }


        }

        [TestMethod]
        public void TestMethod_cc()
        {
            UnitTest_init();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");


            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");
            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");


            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");
            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist3[0] != null && Sentlist1[0] != null && Inboxlist3[0].cc != null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;
                Inboxlist2[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist2[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Sentlist1[0].cc[0].Equals(Address3));
                Assert.IsTrue(Inboxlist3[0] == Sentlist1[0]);
                Assert.IsTrue(Inboxlist2[0].Equals(Sentlist1[0]));

            }
            else
            {
                Assert.Fail("no messages were sent! or cc is null!");
            }


        }


        [TestMethod]
        public void TestMethod_bcc()
        {
            UnitTest_init();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.bcc = new List<MailAddress> { Address3 };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(15000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");
            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist3[0] != null && Sentlist1[0] != null && Inboxlist3[0].bcc != null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;
                Inboxlist2[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist2[0].ThreadId = Sentlist1[0].ThreadId;

                Assert.IsTrue(Sentlist1[0].bcc[0].Equals(Address3));
                Assert.IsTrue(Inboxlist3[0].bcc[0].Address.Equals(Address3.Address));
                Sentlist1[0].bcc = new(); //checking for same mail without bcc in the sent mail
                Assert.IsTrue(Inboxlist2[0].Equals(Sentlist1[0]));

            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        public void TestMethod_reply()
        {

            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            string response = validBody_gen();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            if (Inboxlist3[0] != null)
            {
                MailContent reply = mailHandler3.Reply(Inboxlist3[0]);

                if (reply != null)
                {
                    reply.body = validBody_gen();
                    mailHandler3.Send(reply);
                }
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist1 = GetInbox(mailHandler1, "INBOX");

            List<MailContent> Sentlist3 = GetInbox(mailHandler3, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();
            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist1[0] != null && Sentlist3[0] != null)
            {
                Assert.IsTrue(Sentlist3[0].ThreadId == Inboxlist3[0].ThreadId);
                Sentlist3[0].MessageId = Inboxlist1[0].MessageId;
                Sentlist3[0].ThreadId = Inboxlist1[0].ThreadId;

                //here we also have to set cc to null as reply doesnt include cc
                Sentlist3[0].cc = new();
                //Sentlist3[0].subject =  validSubject;

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
            UnitTest_init();

            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            string response = validBody_gen();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.cc = new List<MailAddress> { Address3 };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            if (Inboxlist3[0] != null)
            {
                MailContent reply = mailHandler3.ReplyAll(Inboxlist3[0]);

                if (reply != null)
                {
                    reply.body = validBody_gen();
                    mailHandler3.Send(reply);
                }
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            // this doesnt make sense. were comparing the original mail sent with the 
            // response sent from the receiver. 

            // i send a mail: knock knock.
            // you reply: who's there.

            // these two mails are not the same. and they're not meant to be
            List<MailContent> Inboxlist1 = GetInbox(mailHandler1, "INBOX");

            List<MailContent> Sentlist3 = GetInbox(mailHandler3, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            if (Inboxlist1[0] != null && Sentlist3[0] != null)
            {
                Assert.IsTrue(Sentlist3[0].ThreadId == Inboxlist3[0].ThreadId);
                Sentlist3[0].MessageId = Inboxlist1[0].MessageId;
                Sentlist3[0].ThreadId = Inboxlist1[0].ThreadId;

                Assert.IsTrue(Sentlist3[0] == Inboxlist1[0]);
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }
        }

        [TestMethod]
        public void TestMethod_forward()
        {
            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            if (Inboxlist2[0] != null)
            {
                MailContent forward = mailHandler2.Forward(Inboxlist2[0]);
                List<MailAddress> fwdlist = new List<MailAddress> { };


                if (forward != null)
                {

                    CollectionAssert.AreEqual(fwdlist, forward.to);
                    Assert.IsTrue(forward.body == $"Forwarded from {Address1}\n " + Inboxlist2[0].body + $" \n\n Originally sent to:{Address2}");
                    forward.to = new List<MailAddress> { Address3 };
                    mailHandler2.Send(forward);
                }
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist3 = GetInbox(mailHandler3, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            //as messageIDs are different for all mail instances, we need to set them equal to compare the mail objects

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

            if (Sentlist1[0] != null && Inboxlist3[0] != null)
            {
                Inboxlist3[0].MessageId = Sentlist1[0].MessageId;
                Inboxlist3[0].ThreadId = Sentlist1[0].ThreadId;
                Assert.IsTrue(Inboxlist3[0].body.Equals($"Forwarded from {Address1.Address}\r\n " + Sentlist1[0].body + $"\r\n\r\n Originally sent to:{Address2.Address}"));
            }
            else
            {
                Assert.Fail("no messages were sent!");
            }


        }

        [TestMethod]
        public void TestMethod_attachment_png()
        {
            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.attachments = new List<string> { validAttachment };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

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
        public void TestMethod_attachment_txt()
        {
            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.attachments = new List<string> { validAttachment1 };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

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
        public void TestMethod_Invalid_attachment()
        {
            UnitTest_init();
            // creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail = new();

            validMail.subject = validSubject;

            validMail.to = new List<MailAddress> { Address2 };

            validMail.from = Address1;

            validMail.body = validBody_gen();

            validMail.attachments = new List<string> { invalidAttachment };

            mut.WaitOne(); Debug.WriteLine("getting mutex access");

            mailHandler1.Send(validMail);

            //let the program sleep for 2 second to make sure the mail is recieved
            System.Threading.Thread.Sleep(10000);

            List<MailContent> Inboxlist2 = GetInbox(mailHandler2, "INBOX");

            List<MailContent> Sentlist1 = GetInbox(mailHandler1, "SENT");

            Debug.WriteLine("finished mutex access"); mut.ReleaseMutex();

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
        "Cant add account twice.")]
        public void TestMethod_Multiple_acc()
        {
            UnitTest_init();
            MailApp.NewAccount(account2);

            MailApp.NewAccount(account1);
            Assert.IsTrue(MailApp.HasAccount());
            MailApp.NewAccount(account1);
        }

        [TestMethod]
        public void TestMethod_DeleteAccount()
        {
            UnitTest_init();
            MailApp MailApp1 = new();
            MailApp1.NewAccount(account1);
            MailApp1.DeleteAccount(account1);
            Assert.IsFalse(MailApp1.HasAccount());

        }


    }
}

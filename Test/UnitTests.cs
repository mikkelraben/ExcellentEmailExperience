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

            //instantiating a GmailHandler object
            IMailHandler mailHandler = new GmailHandler();

            //accessing the password from the environment variables in github?
            string? password = Environment.GetEnvironmentVariable("password");

            //creating a new mail object and sending it to the current mail address? checking for recieving mail
            MailContent validMail =mailHandler.NewMail(validAddress,validSubject,validAddress3,validAddress2,validBody,null);
            
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

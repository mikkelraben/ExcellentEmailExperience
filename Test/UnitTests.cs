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

            IMailHandler mailHandler = new GmailHandler();
            string? password = Environment.GetEnvironmentVariable("password");
            MailContent validMail=mailHandler.NewMail(validAddress,validSubject,validAddress3,validAddress2,validBody,null);
            mailHandler.Send(validMail);
            //mailhandler.()
            //Assert.IsTrue(mailhandler.GetInbox().Length == 0);
        }
    }
}

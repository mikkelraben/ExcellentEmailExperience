﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ExcellentEmailExperience.Interfaces;
using MailKit;
using MailKit.Net.Imap;

namespace ExcellentEmailExperience.Model
{
    internal class MailKitAccount : IAccount
    {
        [JsonInclude]
        public string accountName;
        [JsonInclude]
        public MailAddress mailAddress;

        private string password;
        MailKitHandler handler;

        public MailAddress GetEmail()
        {
            return mailAddress;
        }

        public IMailHandler GetMailHandler()
        {
            return handler;
        }

        public string GetName()
        {
            return accountName;
        }

        public void Login(string email, string secret = "")
        {
            using var client = new ImapClient();
            client.ConnectAsync("imap.friends.com", 993, true);
            try
            {
                client.AuthenticateAsync(email, secret);
            }
            catch (Exception)
            {
                throw new Exception("Login failed");
            }

            handler = new MailKitHandler(email, secret);
            mailAddress = new MailAddress(email);
            password = secret;

            client.DisconnectAsync(true);
        }

        public void Logout()
        {
            throw new NotImplementedException();
        }

        public void SetName(string name)
        {
            accountName = name;
        }

        public bool TryLogin(string email, string secret)
        {
            using var client = new ImapClient();
            client.ConnectAsync("imap.friends.com", 993, true);
            try
            {
                client.AuthenticateAsync(email, secret);
            }
            catch (Exception)
            {
                return false;
            }

            handler = new MailKitHandler(email, secret);
            mailAddress = new MailAddress(email);
            password = secret;

            client.DisconnectAsync(true);
            return true;
        }
    }
}

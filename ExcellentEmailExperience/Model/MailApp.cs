using ExcellentEmailExperience.Interfaces;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public class MailApp
    {
        public readonly List<IAccount> accounts = new();
        public MailApp()
        {
            LoadAccounts();
        }

        /// <summary>
        /// Creates a new account
        /// </summary>
        /// <param name="account">The account to add to the </param>
        public void NewAccount(IAccount account)
        {
            accounts.Add(account);
        }

        public void DeleteAccount()
        {
            throw new NotImplementedException();
        }

        public bool HasAccount()
        {
            return accounts.Count > 0;
        }

        private void LoadAccounts()
        {
            //TODO: Change this when imap is implemented

            foreach (var account in CredentialHandler.GetAccounts())
            {
                GmailAccount gmailAccount = new();
                gmailAccount.Login(account);
                accounts.Add(gmailAccount);
            }
        }
    }
}

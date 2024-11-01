using ExcellentEmailExperience.Interfaces;
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
            //LoadAccounts();
        }

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
            throw new NotImplementedException();
        }
    }
}

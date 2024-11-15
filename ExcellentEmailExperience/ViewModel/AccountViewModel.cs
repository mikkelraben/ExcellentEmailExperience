using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    internal partial class AccountViewModel
    {
        public AccountViewModel(IAccount account)
        {
            this.account = account;
        }
        public IAccount account;
    }

    [ObservableObject]
    internal partial class GmailAccountViewModel : AccountViewModel
    {
        public GmailAccountViewModel(GmailAccount account) : base(account)
        {
            emailAddress = account.mailAddress;
            name = account.GetName();
        }

        [ObservableProperty]
        public string name;
        public MailAddress emailAddress;

        partial void OnNameChanged(string newName)
        {
            account.SetName(newName);
        }
    }

    [ObservableObject]
    internal partial class ImapAccountViewModel : AccountViewModel
    {
        public ImapAccountViewModel(GmailAccount account) : base(account)
        {
            emailAddress = account.mailAddress;
        }

        public MailAddress emailAddress;
    }
}

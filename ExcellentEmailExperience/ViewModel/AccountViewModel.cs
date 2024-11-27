using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    public class AccountViewModel
    {
        public AccountViewModel(IAccount account)
        {
            this.account = account;
        }
        public IAccount account;
        public MailHandlerViewModel mailHandlerViewModel;
    }

    [ObservableObject]
    public partial class GmailAccountViewModel : AccountViewModel
    {

        public GmailAccountViewModel(GmailAccount account, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken) : base(account)
        {
            mailHandlerViewModel = new MailHandlerViewModel(account, dispatcherQueue, cancellationToken);
            emailAddress = account.mailAddress;
            name = account.GetName();
        }

        [ObservableProperty]
        private bool isExpanded = false;

        [ObservableProperty]
        public string name;
        public MailAddress emailAddress;

        partial void OnNameChanged(string newName)
        {
            account.SetName(newName);
        }
    }

    [ObservableObject]
    public partial class ImapAccountViewModel : AccountViewModel
    {
        public ImapAccountViewModel(GmailAccount account) : base(account)
        {
            emailAddress = account.mailAddress;
        }

        public MailAddress emailAddress;
    }
}

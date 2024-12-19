using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    public partial class InboxMail
    {
        public MailAddress from { get; set; }
        public List<MailAddress> to { get; set; }
        public string subject { get; set; }
        public DateTime date { get; set; }

        [ObservableProperty]
        public bool selected = false;

        [ObservableProperty]
        public bool unread = true;

        public string id;

    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.DialProtocol;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    internal partial class FolderViewModel
    {
        public FolderViewModel(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue)
        {
            this.name = name;

            Thread thread = new(() =>
            {
                try
                {
                    foreach (var mail in mailHandler.GetFolder(name, false, false))
                    {
                        var inboxMail = new InboxMail();

                        inboxMail.from = mail.from;
                        inboxMail.to = mail.to;
                        inboxMail.subject = mail.subject;
                        inboxMail.date = mail.date;
                        if (dispatcherQueue == null)
                        {
                            return;
                        }
                        dispatcherQueue.TryEnqueue(() => mails.Add(inboxMail));
                    }
                }
                catch (Exception)
                {
                    // Womp Womp :(
                    return;
                }
            });
            thread.Start();
        }

        string name;
        public ObservableCollection<InboxMail> mails { get; } = new ObservableCollection<InboxMail>();
    }
}

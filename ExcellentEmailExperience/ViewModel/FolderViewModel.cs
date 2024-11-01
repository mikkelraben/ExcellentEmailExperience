using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    internal partial class FolderViewModel
    {
        /// <summary>
        /// Constructor for FolderViewModel
        /// </summary>
        /// <param name="mailHandler">Mailhandler for the account which contains a certain folder</param>
        /// <param name="name">The name of the folder</param>
        /// <param name="dispatcherQueue"></param>
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
                        if (dispatcherQueue != null)
                        {
                            dispatcherQueue.TryEnqueue(() => mails.Add(inboxMail));
                        }
                        else
                        {
                            mails.Add(inboxMail);
                        }
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

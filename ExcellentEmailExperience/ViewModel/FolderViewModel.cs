using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    public partial class FolderViewModel
    {
        /// <summary>
        /// Constructor for FolderViewModel
        /// </summary>
        /// <param name="mailHandler">Mailhandler for the account which contains a certain folder</param>
        /// <param name="name">The name of the folder</param>
        /// <param name="dispatcherQueue"></param>
        public FolderViewModel(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            this.name = name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();

            Thread thread = new(() =>
            {
                try
                {
                    foreach (var mail in mailHandler.GetFolder(name, false, false, 20))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        var inboxMail = new InboxMail();
                        mailsContent.Add(mail);
                        mailsContent.Sort((x, y) => -x.date.CompareTo(y.date));

                        inboxMail.from = mail.from;
                        inboxMail.to = mail.to;
                        inboxMail.subject = mail.subject.Replace("\n", "").Replace("\r", "");
                        inboxMail.date = mail.date.ToLocalTime();
                        if (dispatcherQueue != null)
                        {
                            dispatcherQueue.TryEnqueue(() =>
                            {
                                int insertIndex = mails.Count;

                                for (int i = 0; i < mails.Count; i++)
                                {
                                    if (mails[i].date < inboxMail.date)
                                    {
                                        insertIndex = i;
                                        break;
                                    }
                                }

                                mails.Insert(insertIndex, inboxMail);
                            });
                        }
                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Application probably exited
                    return;
                }
                catch (Exception)
                {
                    MessageHandler.AddMessage("Failed to load mails", MessageSeverity.Error);
                    return;
                }
            });
            thread.Start();
        }

        [ObservableProperty]
        public string name;

        /// <summary>
        /// Collection of mails in the folder used to display in the UI
        /// </summary>
        public ObservableCollection<InboxMail> mails { get; } = new ObservableCollection<InboxMail>();

        /// <summary>
        /// List of mails in the folder currently used to store the mails for the backend
        /// </summary>
        public List<MailContent> mailsContent = new();
    }
}

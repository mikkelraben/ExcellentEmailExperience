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
                        mailsContent.Add(mail);
                        mailsContent.Sort((x, y) => -DateTime.Parse(x.date).CompareTo(DateTime.Parse(y.date)));

                        inboxMail.from = mail.from;
                        inboxMail.to = mail.to;
                        inboxMail.subject = mail.subject;
                        inboxMail.date = mail.date;
                        if (dispatcherQueue == null)
                        {
                            return;
                        }
                        dispatcherQueue.TryEnqueue(() =>
                        {
                            int insertIndex = mails.Count;

                            for (int i = 0; i < mails.Count; i++)
                            {
                                if (DateTime.Parse(mails[i].date) < DateTime.Parse(inboxMail.date))
                                {
                                    insertIndex = i;
                                    break;
                                }
                            }

                            mails.Insert(insertIndex, inboxMail);
                        });

                    }
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // Application probably exited
                    return;
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
        public List<MailContent> mailsContent = new();
    }
}

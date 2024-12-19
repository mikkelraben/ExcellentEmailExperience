using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Google.Apis.Gmail.v1.Data;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    public partial class MailHandlerViewModel
    {
        IAccount mailAccount;
        public MailHandlerViewModel(IAccount account, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            mailAccount = account;
            var mailHandler = account.GetMailHandler();

            if (mailHandler == null)
            {
                return;
            }

            if (mailHandler is GmailHandler handler)
            {
                handler.setAppClose(cancellationToken);
            }

            mailHandler.GetFolderNames().ToList().ForEach(folder => folders.Add(new FolderViewModel(mailHandler, folder, dispatcherQueue, cancellationToken)));
        }

        public void Refresh(DispatcherQueue queue)
        {
            if (queue == null)
            {
                return;
            }
            foreach (var (folder, mail) in mailAccount.GetMailHandler().Refresh(20))
            {

                queue.TryEnqueue(() =>
                {
                    if (mail.Deletion)
                    {
                        folders.First(f => f.FolderName == folder).mailsContent.Remove(mail.email);
                        var mailToDelete = folders.First(f => f.FolderName == folder).mails.First(m => m.id == mail.email.MessageId);
                        folders.First(f => f.FolderName == folder).mails.Remove(mailToDelete);
                    }
                    else
                    {
                        if (mail.flags == MailFlag.none)
                        {
                            folders.First(f => f.FolderName == folder).HandleMessage(mail.email);
                        }
                        else
                        {
                            try
                            {
                                if (mail.flags == MailFlag.unread)
                                {

                                    var unread = folders.First(f => f.FolderName == folder).mails.First(m => m.id == mail.email.MessageId).Unread;
                                    var flip = mail.flags == MailFlag.unread ? !unread : unread;

                                    folders.First(f => f.FolderName == folder).mails.First(m => m.id == mail.email.MessageId).Unread = flip;
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                });
            }
            if (mailAccount.GetMailHandler() is GmailHandler handler)
            {
                handler.UpdateCache();
            }
        }

        public ObservableCollection<FolderViewModel> folders = new();
    }
}

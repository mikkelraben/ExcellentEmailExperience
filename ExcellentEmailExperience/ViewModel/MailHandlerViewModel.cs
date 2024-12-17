using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
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
        DispatcherQueue dispatch;
        CancellationToken cancel;
        public MailHandlerViewModel(IAccount account, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            dispatch = dispatcherQueue;
            cancel = cancellationToken;
            mailAccount = account;
            var mailHandler = account.GetMailHandler();

            if (mailHandler == null)
            {
                return;
            }

            mailHandler.GetFolderNames().ToList().ForEach(folder => folders.Add(new FolderViewModel(mailHandler, folder, dispatcherQueue, cancellationToken)));
        }

        public ObservableCollection<FolderViewModel> folders = new();

        public void Refresh(bool old)
        {

            Thread thread = new(() =>
            {
                ulong[] ids = mailAccount.GetMailHandler().GetNewIds();

                // TODO: unimplement newest cus we have to refresh old mail too to check if unread
                ulong NewestId = ids[0];

                // TODO: cant lastid be passed from the view (mainwindow.xaml)? If so unimplement GetNewIds?
                ulong LastId = ids[0];

                foreach (var folder in folders)
                {
                    folder.UpdateViewMails(mailAccount.GetMailHandler(), folder.FolderName, dispatch, cancel, old, LastId, NewestId);
                }

                // TODO: kan den her linje slettes???
                ids = mailAccount.GetMailHandler().GetNewIds();
            });
            thread.Start();
        }
    }
}

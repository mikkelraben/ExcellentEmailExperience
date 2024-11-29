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
        public MailHandlerViewModel(IAccount account, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            var mailHandler = account.GetMailHandler();

            if (mailHandler == null)
            {
                return;
            }

            mailHandler.GetFolderNames().ToList().ForEach(folder => folders.Add(new FolderViewModel(mailHandler, folder, dispatcherQueue, cancellationToken)));
        }

        public ObservableCollection<FolderViewModel> folders = new();

    }
}

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
    internal partial class MailHandlerViewModel
    {
        private readonly IMailHandler mailHandler;
        public MailHandlerViewModel(string accountName, IMailHandler mailHandler, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            this.mailHandler = mailHandler;
            mailHandler.GetFolderNames().ToList().ForEach(folder => folders.Add(new FolderViewModel(mailHandler, folder, dispatcherQueue, cancellationToken)));
            name = accountName;
        }

        public ObservableCollection<FolderViewModel> folders = new();
        public string name;

    }
}

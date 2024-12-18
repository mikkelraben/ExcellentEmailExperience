using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Foundation;


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
        /// 
        IMailHandler MailHandler;
        DispatcherQueue DispatchQueue;
        CancellationToken CancelToken;

        // DO NOT USE ON UI SIDE. THIS IS INTERNAL 
        public string FolderName;

        /// <summary>
        /// Constructor for FolderViewModel if mails should be loaded
        /// </summary>
        /// <param name="mailHandler"></param>
        /// <param name="name"></param>
        /// <param name="dispatcherQueue"></param>
        /// <param name="cancellationToken"></param>
        public FolderViewModel(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            mails = new(this);
            FolderName = name;
            DispatchQueue = dispatcherQueue;
            CancelToken = cancellationToken;
            MailHandler = mailHandler;

            this.name = name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();
            this.mailHandler = mailHandler;


            Thread thread = new(() =>
            {
                GetViewMails(
                    mailHandler,
                    name,
                    dispatcherQueue,
                    cancellationToken
                    );
            });
            thread.Start();
        }

        /// <summary>
        /// Constructor for FolderViewModel if this folder is for searching
        /// </summary>
        public FolderViewModel(IEnumerable<MailContent> mailContents, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            mails = new(this);
            this.Name = "Search";

            foreach (var mail in mailContents)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                HandleMessage(dispatcherQueue, mail, CancellationToken.None);
            }
        }

        private void GetViewMails(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var mail in mailHandler.GetFolder(name, 20))
                {
                    HandleMessage(dispatcherQueue, mail, cancellationToken);
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
        }

        private void HandleMessage(DispatcherQueue dispatcherQueue, MailContent mail, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            var inboxMail = new InboxMail();
            mailsContent.Add(mail);
            mailsContent.Sort((x, y) => -x.date.CompareTo(y.date));

            if (mail.from != null)
            {
                if (mail.from.DisplayName == "")
                {
                    inboxMail.from = new System.Net.Mail.MailAddress(mail.from.Address, mail.from.Address);
                }
                else
                {
                    inboxMail.from = mail.from;
                }
            }
            inboxMail.to = mail.to;
            inboxMail.subject = mail.subject.Replace("\n", "").Replace("\r", "");
            inboxMail.date = mail.date.ToLocalTime();
            inboxMail.Unread = mail.flags.HasFlag(MailFlag.unread);
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



        [ObservableProperty]
        public string name;

        /// <summary>
        /// Collection of mails in the folder used to display in the UI
        /// </summary>
        public FolderCollection mails { get; }

        /// <summary>
        /// List of mails in the folder currently used to store the mails for the backend
        /// </summary>
        public List<MailContent> mailsContent = new();

        public IMailHandler mailHandler;
    }

    public class FolderCollection : ObservableCollection<InboxMail>, ISupportIncrementalLoading
    {
        FolderViewModel folderViewModel;
        private bool _busy;

        public FolderCollection(FolderViewModel viewModel)
        {
            folderViewModel = viewModel;
        }
        public bool HasMoreItems { get; private set; } = true;
        public bool IsLoading { get; private set; } = false;

        bool ISupportIncrementalLoading.HasMoreItems => HasMoreItems;

        IAsyncOperation<LoadMoreItemsResult> ISupportIncrementalLoading.LoadMoreItemsAsync(uint count)
        {
            if (_busy)
            {
                throw new InvalidOperationException("Only one operation in flight at a time");
            }

            _busy = true;
            return AsyncInfo.Run(async (cancellationToken) =>
            {
                await Task.Run(() =>
                {
                    var ids = folderViewModel.mailHandler.GetNewIds();


                    folderViewModel.UpdateViewMails(true, ids[1], ids[0]);
                    _busy = false;
                });

                return new LoadMoreItemsResult { Count = count };
            });
        }
    }
}

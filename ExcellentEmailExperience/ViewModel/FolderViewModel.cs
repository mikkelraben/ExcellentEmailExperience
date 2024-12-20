using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
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
            DispatchQueue = dispatcherQueue;
            CancelToken = cancellationToken;

            foreach (var mail in mailContents)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                HandleMessage(mail);
            }
        }

        private void GetViewMails(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var mail in mailHandler.GetFolder(name, 20))
                {
                    HandleMessage(mail);
                    initialized = true;
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

        public void HandleMessage(MailContent mail)
        {
            //prevents mail duplicate badness
            if (mailsContent.Exists(x => x.MessageId == mail.MessageId))
            {
                return;
                throw new ArgumentException("mail already exists in viewmodel");
            }

            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            mailsContent.Add(mail);
            mailsContent.Sort((x, y) => -x.date.CompareTo(y.date));

            InboxMail inboxMail = CreateInboxMail(mail);
            if (DispatchQueue != null)
            {
                DispatchQueue.TryEnqueue(() =>
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

        private static InboxMail CreateInboxMail(MailContent mail)
        {
            var inboxMail = new InboxMail();

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
            inboxMail.id = mail.MessageId;
            return inboxMail;
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
        public bool initialized = false;
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
            return AsyncInfo.Run((cancellationToken) => LoadMoreItemsResult(count));
        }

        async Task<LoadMoreItemsResult> LoadMoreItemsResult(uint count)
        {
            _busy = true;
            if (!folderViewModel.initialized)
            {
                return new LoadMoreItemsResult();
            }
            if (folderViewModel.mailsContent.Count == 0)
            {
                HasMoreItems = false;
                return new LoadMoreItemsResult();
            }

            bool hasMoreItems = false;
            IsLoading = true;
            await Task.Run(() =>
            {
                foreach (var mail in folderViewModel.mailHandler.RefreshOld(folderViewModel.FolderName, 20, folderViewModel.mailsContent[folderViewModel.mailsContent.Count - 1].date))
                {
                    hasMoreItems = true;
                    if (!mail.Deletion)
                    {
                        folderViewModel.HandleMessage(mail.email);
                    }
                }
                _busy = false;
                IsLoading = false;
                HasMoreItems = hasMoreItems;
                return new LoadMoreItemsResult { Count = count };
            });

            return new LoadMoreItemsResult();
        }
    }
}

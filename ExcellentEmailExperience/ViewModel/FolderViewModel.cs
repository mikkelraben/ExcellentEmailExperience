using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Google;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            if (mailHandler is GmailHandler handler)
            {
                name = handler.GetFolderName(name);
            }

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
                HandleMessage(mail, 2000);
            }
        }

        private void GetViewMails(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var mail in mailHandler.GetFolder(FolderName, 20))
                {
                    HandleMessage(mail, 20);
                }
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // Application probably exited
            }
            catch (GoogleApiException e)
            {
                MessageHandler.AddMessage($"Failed to connect to google: {e}", MessageSeverity.Error);
            }
            catch (Exception e)
            {
                MessageHandler.AddMessage($"Failed to load mails: {e}", MessageSeverity.Error);
            }
            initialized = true;
        }

        Mutex mailsMutex = new();
        public void HandleMessage(MailContent mail, int count)
        {

            //prevents mail duplicate badness
            if (CancelToken.IsCancellationRequested)
            {
                return;
            }
            if (!mailsContent.Exists(x => x.MessageId == mail.MessageId))
            {
                mailsContent.Add(mail);
                mailsContent.Sort((x, y) => -x.date.CompareTo(y.date));
            }

            InboxMail inboxMail = CreateInboxMail(mail);

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


                mailsMutex.WaitOne();
                if (mails.Any(m => m.id == inboxMail.id))
                {
                    mailsMutex.ReleaseMutex();
                    return;
                }

                mails.Insert(insertIndex, inboxMail);
                if (mails.Count > count)
                {
                    mails.RemoveAt(mails.Count - 1);
                }
                mailsMutex.ReleaseMutex();
            });
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
            while (!folderViewModel.initialized)
            {
                await Task.Delay(100);
            }
            await Task.Delay(100);
            if (folderViewModel.mails.Count == 0)
            {
                HasMoreItems = false;
                return new LoadMoreItemsResult();
            }

            bool hasMoreItems = false;
            IsLoading = true;
            int loadCount = await Task.Run(() =>
            {
                int loadCount = 0;
                foreach (var mail in folderViewModel.mailHandler.RefreshOld(folderViewModel.FolderName, 20, folderViewModel.mails[folderViewModel.mails.Count - 1].date))
                {
                    hasMoreItems = true;
                    if (!mail.Deletion)
                    {
                        loadCount++;
                        folderViewModel.HandleMessage(mail.email, 2000);
                    }
                }
                _busy = false;
                IsLoading = false;
                HasMoreItems = hasMoreItems;
                return loadCount;
            });

            return new LoadMoreItemsResult((uint)loadCount);
        }
    }
}

﻿using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Principal;
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
        /// 
        IMailHandler MailHandler;
        DispatcherQueue DispatchQueue;
        CancellationToken CancelToken;

        // DO NOT USE ON UI SIDE. THIS IS INTERNAL 
        string FolderName;
        public FolderViewModel(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken)
        {
            FolderName = name;
            DispatchQueue = dispatcherQueue;
            CancelToken = cancellationToken;
            MailHandler = mailHandler;
  
            this.name = name.Substring(0, 1).ToUpper() + name.Substring(1).ToLower();

            Thread thread = new(() =>
            {
                UpdateViewMails(
                    mailHandler,
                    name,
                    dispatcherQueue,
                    cancellationToken,
                    false,
                    false
                    );
            });
            thread.Start();
        }

        private void UpdateViewMails(IMailHandler mailHandler, string name, DispatcherQueue dispatcherQueue, CancellationToken cancellationToken, bool old, bool refresh)
        {
            try
            {
                foreach (var mail in mailHandler.GetFolder(name,old, refresh, 20))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    var inboxMail = new InboxMail();
                    mailsContent.Add(mail);
                    mailsContent.Sort((x, y) => -x.date.CompareTo(y.date));

                        if (mail.from.DisplayName == "")
                        {
                            inboxMail.from = new System.Net.Mail.MailAddress(mail.from.Address, mail.from.Address);
                        }
                        else
                        {
                            inboxMail.from = mail.from;
                        }

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
        }

        public void RefreshFolder()
        {
            
            Thread thread = new(() =>
            {
                UpdateViewMails(
                MailHandler,
                FolderName,
                DispatchQueue,
                CancelToken,
                false,
                true
                );

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

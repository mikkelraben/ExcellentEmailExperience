﻿using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    public partial class UserMessageViewModel
    {
        public UserMessageViewModel(DispatcherQueue dispatcherQueue)
        {
            MessageHandler.GetMessages().ForEach(message => messages.Add(message));
            MessageHandler.MessageAdded += (sender, e) =>
            {
                dispatcherQueue.TryEnqueue(() =>
                {
                    messages.Insert(0, MessageHandler.GetFirstMessage());
                });
            };
            messages.CollectionChanged += (sender, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    var oldMessage = e.OldItems?.Cast<Message>().First();
                    MessageHandler.GetMessages().Remove(oldMessage);
                }
            };
        }

        public ObservableCollection<Message> messages = new();

    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using System.Threading;

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
                    var message = MessageHandler.GetFirstMessage();
                    messages.Insert(0, message);

                    Thread thread = new(() =>
                    {
                        Task.Delay(10000).Wait();
                        dispatcherQueue.TryEnqueue(() => { messages.Remove(message); });
                    });
                    thread.Start();
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

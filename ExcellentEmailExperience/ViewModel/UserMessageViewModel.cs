using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    public partial class UserMessageViewModel
    {
        public UserMessageViewModel()
        {
            MessageHandler.GetMessages().ForEach(message => messages.Add(message));
            MessageHandler.MessageAdded += (sender, e) =>
            {
                messages.Insert(0, MessageHandler.GetFirstMessage());
            };
        }

        public ObservableCollection<Message> messages = new();

    }
}

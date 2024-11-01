﻿using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.ViewModel
{
    [ObservableObject]
    internal partial class InboxMail
    {
        public MailAddress from { get; set; }
        public MailAddress[] to { get; set; }
        public string subject { get; set; }
        public string date { get; set; }

        [ObservableProperty]
        public bool selected = false;

    }
}
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ExcellentEmailExperience.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Intro : WinUIEx.WindowEx
    {
        MailApp mailApp;
        public Intro(MailApp mailApp)
        {
            this.mailApp = mailApp;
            this.InitializeComponent();
            this.CenterOnScreen();
            ExtendsContentIntoTitleBar = true;

            this.IsResizable = false;
            this.IsMaximizable = false;
            this.IsMinimizable = false;
            TitleText.Opacity = 1.0;
            RemoveTitleText();
        }
        private void RemoveTitleText()
        {
            new Thread(() =>
            {
                Thread.Sleep(2000);

                if (DispatcherQueue == null)
                {
                    return;
                }
                DispatcherQueue.TryEnqueue(() =>
                {
                    TitleText.Opacity = 0.0;
                    SubtitleText.Opacity = 1.0;
                    GetStartedButton.Opacity = 1.0;
                });
            }).Start();
        }

        public EventHandler FirstAccountCreated;

        private void GetStartedButton_Click(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                IAccount account = new GmailAccount();

                account.Login("user");

                mailApp.NewAccount(account);

                FirstAccountCreated.Invoke(this, new EventArgs());
                DispatcherQueue.TryEnqueue(() =>
                {
                    Close();
                });
            }).Start();
        }
    }
}

using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ExcellentEmailExperience.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsWindow : WinUIEx.WindowEx
    {
        ObservableCollection<AccountViewModel> accounts;
        MailApp mailApp;
        CancellationToken appClose;

        public SettingsWindow(MailApp mailApp, ObservableCollection<AccountViewModel> accounts, CancellationToken appClose)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.InitializeComponent();
                this.appClose = appClose;
                this.ExtendsContentIntoTitleBar = true;

                this.MinWidth = 500;
                this.MinHeight = 300;
                this.accounts = accounts;
                AccountsListView.ItemsSource = this.accounts;

                this.mailApp = mailApp;

                Version.Text = "Version: " + AppInfo.Current.Package.Id.Version.Major + "." + AppInfo.Current.Package.Id.Version.Minor + "." + AppInfo.Current.Package.Id.Version.Build + "." + AppInfo.Current.Package.Id.Version.Revision;

                Closed += (sender, e) =>
                {
                    mailApp.SaveAccounts();
                };
            });
        }

        public void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            GmailAccount account = new GmailAccount();

            account.Login(null);

            mailApp.NewAccount(account);

            account.SetName("New Account");

            accounts.Add(new GmailAccountViewModel(account, DispatcherQueue, appClose));
        }
    }
}

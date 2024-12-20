using ExcellentEmailExperience.Helpers;
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
            this.mailApp = mailApp;
            this.accounts = accounts;
            DispatcherQueue.TryEnqueue(() =>
            {
                this.InitializeComponent();

                TitleBarHelper.ConfigureTitleBar(mailApp, AppWindow.TitleBar);

                ThemeComboBox.SelectedIndex = (int)mailApp.settings.theme;
                this.appClose = appClose;
                this.ExtendsContentIntoTitleBar = true;

                this.MinWidth = 500;
                this.MinHeight = 300;
                AccountsListView.ItemsSource = this.accounts;


                Version.Text = "Version: " + AppInfo.Current.Package.Id.Version.Major + "." + AppInfo.Current.Package.Id.Version.Minor + "." + AppInfo.Current.Package.Id.Version.Build + "." + AppInfo.Current.Package.Id.Version.Revision;

                Closed += (sender, e) =>
                {
                    mailApp.SaveAccounts();
                    mailApp.SaveAppSettings();
                };
            });
        }

        public void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            AccountLoadingRing.IsActive = true;
            Thread thread = new(() =>
            {
                GmailAccount account = new GmailAccount();

                account.Login(null);

                bool exists = false;
                foreach (var otherAccount in accounts)
                {
                    if (otherAccount.account.GetEmail().Address == account.mailAddress.Address)
                    {
                        exists = true;
                        MessageHandler.AddMessage("Account already exists", MessageSeverity.Error);
                        break;
                    }
                }
                if (!exists)
                    mailApp.NewAccount(account);

                account.SetName("New Account");
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (!exists)
                        accounts.Add(new GmailAccountViewModel(account, DispatcherQueue, appClose));
                    AccountLoadingRing.IsActive = false;
                });
            });
            thread.Start();
        }

        private void RemoveAccount_Click(object sender, RoutedEventArgs e)
        {
            object account = (sender as Button).DataContext;
            accounts.Remove(account as AccountViewModel);
            mailApp.DeleteAccount((account as AccountViewModel).account);
        }

        private void ThemeSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            switch (ThemeComboBox.SelectedIndex)
            {
                case 0:
                    mailApp.settings.theme = Theme.System;
                    break;
                case 1:
                    mailApp.settings.theme = Theme.Light;
                    break;
                case 2:
                    mailApp.settings.theme = Theme.Dark;
                    break;
            }
            mailApp.SaveAppSettings();
        }
    }
}

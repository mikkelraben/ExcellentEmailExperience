using CommunityToolkit.Mvvm.ComponentModel;
using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ExcellentEmailExperience.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Email : Page
    {
        MailContentViewModel viewModel = new();
        public Email()
        {
            this.InitializeComponent();
        }

        public async Task Initialize()
        {
            HTMLViewer.CanGoBack = false;
            HTMLViewer.CanGoForward = false;
            await HTMLViewer.EnsureCoreWebView2Async();

            HTMLViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            HTMLViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            HTMLViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            HTMLViewer.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            HTMLViewer.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }

        private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
        {
            Uri uri = new Uri(args.Uri);
            args.Handled = true;
            _ = Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            try
            {
                if (args.Uri.StartsWith("http://") || args.Uri.StartsWith("https://"))
                {
                    Uri uri = new Uri(args.Uri);
                    args.Cancel = true;
                    _ = Windows.System.Launcher.LaunchUriAsync(uri);
                }
            }
            catch (UriFormatException)
            {
                // Do nothing
            }
        }

        public void ChangeMail(MailContent mail, bool editable)
        {
            viewModel.Update(mail);
            viewModel.IsEditable = editable;

            EmptyMail.Visibility = Visibility.Collapsed;
            switch (mail.bodyType)
            {
                case BodyType.Plain:
                    MailContent.Text = mail.body;
                    HTMLViewer.Visibility = Visibility.Collapsed;
                    ScrollView.Visibility = Visibility.Visible;
                    break;
                case BodyType.Html:
                    HTMLViewer.Visibility = Visibility.Visible;
                    ScrollView.Visibility = Visibility.Collapsed;

                    HTMLViewer.NavigateToString(mail.body);
                    break;
            }
        }

        private void ClickFrom(object sender, PointerRoutedEventArgs e)
        {

        }

        private void FromAddress_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {

        }
    }
}


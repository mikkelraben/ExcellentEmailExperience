using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

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
        ObservableCollection<AccountViewModel> accounts;
        public Email(ObservableCollection<AccountViewModel> accounts)
        {
            this.accounts = accounts;
            this.InitializeComponent();
        }

        public async Task Initialize()
        {
            HTMLViewer.CanGoBack = false;
            HTMLViewer.CanGoForward = false;
            await HTMLViewer.EnsureCoreWebView2Async();

#if !DEBUG
            HTMLViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            HTMLViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
            HTMLViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsScriptEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsSwipeNavigationEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            HTMLViewer.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            HTMLViewer.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            HTMLViewer.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            HTMLViewer.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            HTMLViewer.CoreWebView2.AddWebResourceRequestedFilter("cid:*", CoreWebView2WebResourceContext.All);
        }

        private async void CoreWebView2_WebResourceRequested(CoreWebView2 sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                var cid = args.Request.Uri.Replace("cid:", "");
                var path = @$"{folder.Path}\attachments\{viewModel.messageId}\{Convert.ToHexString(Encoding.UTF8.GetBytes(cid))}";

                FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                CoreWebView2WebResourceResponse response = HTMLViewer.CoreWebView2.Environment.CreateWebResourceResponse(fileStream.AsRandomAccessStream(), 200, "OK", "image/png");
                args.Response = response;
            }
            catch (Exception)
            {
                CoreWebView2WebResourceResponse response = HTMLViewer.CoreWebView2.Environment.CreateWebResourceResponse(null, 404, "OK", "");
                args.Response = response;
            }
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

            Editor.Visibility = editable ? Visibility.Visible : Visibility.Collapsed;

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

        private async void SaveAttachment(object sender, RoutedEventArgs e)
        {
            string path = ((e.OriginalSource as MenuFlyoutItem).DataContext as string);


            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            savePicker.SuggestedFileName = Path.GetFileName(path);
            savePicker.FileTypeChoices.Add("Attachment", new List<string>() { Path.GetExtension(path) });

            var window = App.mainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

            var file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                StorageFile storageFile = await StorageFile.GetFileFromPathAsync(path);
                await storageFile.CopyAndReplaceAsync(file);
            }
        }

        private void FromAddress_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddMailAddress_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.IsEditable)
                return;

            viewModel.recipients.Add(new(""));
        }

        private void RemoveMailAddress_Click(object sender, RoutedEventArgs e)
        {
            var stringThing = (sender as Button).DataContext as StringWrapper;
            viewModel.recipients.Remove(stringThing);
        }

        private void SendMail_Click(object sender, RoutedEventArgs e)
        {
            AccountViewModel account = FromAddress.SelectedItem as AccountViewModel;
            if (account == null)
            {
                MessageHandler.AddMessage("Could not send from no account", MessageSeverity.Error);
                return;
            }

            MailContent mail = new();
            mail.from = account.account.GetEmail();
            foreach (var recipient in viewModel.recipients)
            {
                mail.to.Add(new(recipient.Value));
            }
            mail.subject = viewModel.Subject;
            mail.body = "Hello There this is mail";
            mail.bodyType = BodyType.Plain;
            mail.attachments = viewModel.Attachments;
            mail.cc = viewModel.Cc;
            mail.bcc = viewModel.Bcc;
            try
            {
                account.account.GetMailHandler().Send(mail);
                MessageHandler.AddMessage("Sent Message", MessageSeverity.Success);
            }
            catch (ArgumentException _)
            {

            }
        }
    }

    public class CountToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToNotVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}


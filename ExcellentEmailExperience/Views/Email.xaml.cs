using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI.Text;
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
        Visibility displayCC = Visibility.Collapsed;

        public Email(ObservableCollection<AccountViewModel> accounts)
        {
            this.accounts = accounts;
            this.InitializeComponent();
        }

        public async Task Initialize()
        {

            CoreWebView2EnvironmentOptions options = new();
            options.AreBrowserExtensionsEnabled = true;
            options.EnableTrackingPrevention = true;
            CoreWebView2Environment environment = await CoreWebView2Environment.CreateWithOptionsAsync(null, null, options);

            HTMLViewer.CanGoBack = false;
            HTMLViewer.CanGoForward = false;
            await HTMLViewer.EnsureCoreWebView2Async(environment);


            HTMLViewer.CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Strict;

#if !DEBUG
            HTMLViewer.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            HTMLViewer.CoreWebView2.Settings.AreDevToolsEnabled = false;
#endif
            HTMLViewer.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
            //HTMLViewer.CoreWebView2.Settings.IsScriptEnabled = false;
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

                    var file = StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Extensions/DarkReader.txt")).AsTask().Result;
                    var script = Windows.Storage.FileIO.ReadTextAsync(file).AsTask().Result;

                    if (ActualTheme == ElementTheme.Dark)
                    {
                        HTMLViewer.CoreWebView2.ExecuteScriptAsync(script);
                    }

                    break;
            }
        }

        private void ClickFrom(object sender, PointerRoutedEventArgs e)
        {

        }

        private void FromAddress_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
        {
            if (!viewModel.IsEditable)
            {
                ChangeMail(new(), true);
            }
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
            mail.cc = viewModel.Cc;
            mail.bcc = viewModel.Bcc;
            try
            {
                foreach (var recipient in viewModel.recipients)
                {
                    mail.to.Add(new(recipient.Value));
                }
            }
            catch (Exception _)
            {
                MessageHandler.AddMessage("A recipient mail is not valid please check your To field", MessageSeverity.Error);
                return;
            }
            try
            {
                foreach (var recipient in viewModel.ccStrings)
                {
                    mail.cc.Add(new(recipient.Value));
                }

            }
            catch (Exception)
            {
                MessageHandler.AddMessage("A recipient mail is not valid please check your Cc field", MessageSeverity.Error);
                return;
            }

            try
            {
                foreach (var recipient in viewModel.bccStrings)
                {
                    mail.bcc.Add(new(recipient.Value));
                }

            }
            catch (Exception)
            {
                MessageHandler.AddMessage("A recipient mail is not valid please check your Bcc field", MessageSeverity.Error);
                return;
            }


            if (mail.to.Count == 0)
            {
                MessageHandler.AddMessage("You need to have at least one recipient", MessageSeverity.Error);
                return;
            }


            mail.subject = viewModel.Subject;
            mail.bodyType = BodyType.Html;
            mail.attachments = viewModel.Attachments;


            Stream bla = new MemoryStream();
            MailEditor.Document.SaveToStream(TextGetOptions.None, bla.AsRandomAccessStream());
            //MailEditor.Document.SaveToStream(TextGetOptions.FormatRtf, new FileStream(@$"C:\Users\mikke\Desktop\bla.rtf", FileMode.OpenOrCreate).AsRandomAccessStream());
            string rtf = Encoding.UTF8.GetString((bla as MemoryStream).ToArray());

            //mail.body = Rtf.ToHtml(bla);
            MailEditor.Document.GetText(TextGetOptions.None, out mail.body);

            mail.body = mail.body + viewModel.Body;

            try
            {
                account.account.GetMailHandler().Send(mail);
                MessageHandler.AddMessage("Sent Message", MessageSeverity.Success);
                ChangeMail(new(), false);
            }
            catch (Exception _)
            {
                MessageHandler.AddMessage("Could not send message", MessageSeverity.Error);
            }
        }

        private void CC_Expand(object sender, RoutedEventArgs e)
        {
            displayCC = displayCC == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            CCField.Visibility = displayCC;
            CCText.Visibility = displayCC;
            BCCField.Visibility = displayCC;
            BCCText.Visibility = displayCC;
        }

        private void AddCC_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.IsEditable)
                return;

            viewModel.ccStrings.Add(new(""));
        }

        private void RemoveCC_Click(object sender, RoutedEventArgs e)
        {
            var stringThing = (sender as Button).DataContext as StringWrapper;
            viewModel.ccStrings.Remove(stringThing);
        }

        private void RemoveBCC_Click(object sender, RoutedEventArgs e)
        {
            var stringThing = (sender as Button).DataContext as StringWrapper;
            viewModel.bccStrings.Remove(stringThing);
        }

        private void AddBCC_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.IsEditable)
                return;

            viewModel.bccStrings.Add(new(""));

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


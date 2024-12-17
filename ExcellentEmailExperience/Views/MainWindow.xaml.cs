using ExcellentEmailExperience.Helpers;
using ExcellentEmailExperience.Interfaces;
using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ExcellentEmailExperience.Views
{

    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : WinUIEx.WindowEx
    {
        MailApp mailApp;
        FolderViewModel currentFolder;
        ObservableCollection<AccountViewModel> accounts = new();
        CancellationTokenSource cancellationToken = new();
        UserMessageViewModel MessageViewModel;
        public static List<object> DraggedItems = new();

        public MainWindow(MailApp mailApp)
        {
            this.mailApp = mailApp;

            this.InitializeComponent();
            MessageViewModel = new(DispatcherQueue);
            this.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            Titlebar.Loaded += Titlebar_Loaded;

            Title = "EEE";

            this.MinWidth = 760;
            this.MinHeight = 450;

            //if the the app is not in debug mode then collapse the subtitle
#if !DEBUG
            Subtitle.Visibility = Visibility.Collapsed;
#endif

            TitleBarHelper.ConfigureTitleBar(mailApp, AppWindow.TitleBar);

            mailApp.Accounts.ForEach(account =>
            {
                if (account is GmailAccount gmail)
                {
                    accounts.Add(new GmailAccountViewModel(gmail, DispatcherQueue, cancellationToken.Token));
                }
            });

            Closed += (s, e) => cancellationToken.Cancel();

            mailApp.SaveAccounts();
            mailApp.SaveAppSettings();

            Email email = new(accounts);
            Task task = email.Initialize();
            MainFrame.Content = email;

            SizeChanged += MainWindow_SizeChanged;

            if (accounts.Count == 0)
            {
                MessageHandler.AddMessage("No accounts found", MessageSeverity.Error);
                return;
            }
            if (accounts[0].mailHandlerViewModel.folders.Count == 0)
            {
                MessageHandler.AddMessage("No folders found", MessageSeverity.Error);
                return;
            }
            currentFolder = accounts[0].mailHandlerViewModel.folders[0];

            FolderName.Text = currentFolder.Name;

            MailList.ItemsSource = currentFolder.mails;

            accounts.CollectionChanged += (s, e) =>
            {
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
                {
                    mailApp.Accounts.Remove((e.OldItems[0] as AccountViewModel).account);
                }
                if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
                {
                    if (mailApp.Accounts.Contains((e.NewItems[0] as AccountViewModel).account))
                    {
                        return;
                    }

                    mailApp.Accounts.Insert(e.NewStartingIndex, (e.NewItems[0] as AccountViewModel).account);
                    mailApp.SaveAccounts();
                }
            };

        }


        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            SetPassthroughRegion();
        }

        private void Titlebar_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SetPassthroughRegion();
        }

        private void SetPassthroughRegion()
        {
            InputNonClientPointerSource inputNonClientPointerSource = InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);

            List<RectInt32> rects = new();

            double scaleAdjustment = Titlebar.XamlRoot.RasterizationScale;

            UIElementCollection titleChildren = Titlebar.Children;



            foreach (var child in titleChildren)
            {
                if (child is AppBarButton || child is Button)
                {
                    System.Numerics.Vector2 sizeVector = child.ActualSize;

                    Rect rect = child.TransformToVisual(null).TransformBounds(new Rect(0, 0,
                                                         sizeVector.X,
                                                         sizeVector.Y));

                    rects.Add(new RectInt32((int)(rect.X * scaleAdjustment),
                                            (int)(rect.Y * scaleAdjustment),
                                            (int)(rect.Width * scaleAdjustment),
                                            (int)(rect.Height * scaleAdjustment)));

                }
            }

            inputNonClientPointerSource.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());

        }

        private void MailList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCount = MailList.SelectedItems.Count;

            if (selectedCount == 1)
            {
                var selectedMail = MailList.SelectedItem as InboxMail;
                MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

                (MainFrame.Content as Email).ChangeMail(mailContent, false);
                MassEditMenu.Visibility = Visibility.Collapsed;
                if (selectedMail.Unread)
                {
                    _ = currentFolder.mailHandler.UpdateFlag(mailContent, MailFlag.unread);

                    currentFolder.mails[MailList.SelectedIndex].Unread = false;
                }

            }
            else if (selectedCount > 1)
            {
                MassEditMenu.Visibility = Visibility.Visible;
            }

            foreach (var mail in e.RemovedItems)
            {
                (mail as InboxMail).Selected = false;
            }

            foreach (var mail in MailList.SelectedItems)
            {
                (mail as InboxMail).Selected = true;
            }

            if (SidebarLarge)
                SidebarLarge = false;
        }

        private void MailBox_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            RefreshButton.Opacity = 1;
        }

        private void MailBox_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            RefreshButton.Opacity = 1;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            accounts[0].mailHandlerViewModel.Refresh(false);
        }

        bool sidebarLarge = false;

        public bool SidebarLarge
        {
            get => sidebarLarge; set
            {
                sidebarLarge = value;
                Siderbar.Margin = value ? new(0, 0, 0, 0) : new(-200, 0, 0, 0);

            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            SidebarLarge = !SidebarLarge;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        bool settingsActive = false;
        SettingsWindow settings;

        private void OpenSettings()
        {

            if (settingsActive)
            {
                if (settings.Visible)
                    settings.BringToFront();
                return;
            }

            settings = new(mailApp, accounts, cancellationToken.Token);
            settings.SetIsMaximizable(false);
            settings.SetIsMinimizable(false);

            settings.MoveAndResize(AppWindow.Position.X + 32, AppWindow.Position.Y + 32, Width - 64, Height - 64);

            settings.Activate();
            settingsActive = true;
            settings.Closed += (s, e) => settingsActive = false;
            Closed += (s, e) => settings.Close();
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SidebarLarge = false;
        }

        private void NewMail_Click(object sender, RoutedEventArgs e)
        {
            MailContent mailContent = new MailContent();

            mailContent.from = mailApp.Accounts[0].GetEmail();

            (MainFrame.Content as Email).ChangeMail(mailContent, true);

        }

        private void Siderbar_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count == 0)
            {
                currentFolder = null;
                FolderName.Text = "No Folder";
                MailList.ItemsSource = null;
                SidebarLarge = false;
                MessageHandler.AddMessage("No folder selected", MessageSeverity.Error);

                return;
            }
            if (args.AddedItems.Count != 1)
            {
                MessageHandler.AddMessage("Multiple items selected", MessageSeverity.Error);
                return;
            }
            if (args.AddedItems[0] is FolderViewModel folder)
            {
                currentFolder = folder;
                FolderName.Text = currentFolder.Name;
                MailList.ItemsSource = currentFolder.mails;
                SidebarLarge = false;
            }
        }

        private void MessagesBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            sender.IsOpen = true;
            MessageViewModel.messages.Remove(sender.DataContext as Message);
        }

        private void MessagesBar_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
        {
            throw new NotImplementedException();
            var infoBar = (sender as InfoBar);
            var message = infoBar.DataContext as Message;
            if (message is null)
                return;

            MessageSeverityToInfoBarSeverity converter = new();
            infoBar.Severity = (InfoBarSeverity)converter.Convert(message.severity, typeof(MessageSeverity), null, null);

        }

        private void Siderbar_DragItemsStarting(TreeView sender, TreeViewDragItemsStartingEventArgs args)
        {
            if (args.Items[0] is not AccountViewModel)
            {
                args.Cancel = true;
                return;
            }

            foreach (var account in accounts)
            {
                var item = sender.ContainerFromItem(account);
                if (item is TreeViewItem treeViewItem)
                {
                    treeViewItem.IsExpanded = false;
                }
                Debug.WriteLine(item);
            }

            foreach (var item in args.Items)
            {
                DraggedItems.Add(item);
            }
        }

        private void Siderbar_DragItemsCompleted(TreeView sender, TreeViewDragItemsCompletedEventArgs args)
        {
            DraggedItems.Clear();
        }

        private void TreeViewItem_DragEnter(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.None;
            Debug.WriteLine(e);
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            var search = sender.Text;

            if (search == "")
            {
                MessageHandler.AddMessage("No search query", MessageSeverity.Info);
                return;
            }

            var mails = currentFolder.mailHandler.Search(search, 20);

            FolderViewModel searchFolder = new(mails, DispatcherQueue, cancellationToken.Token);

            searchFolder.mailHandler = currentFolder.mailHandler;
            currentFolder = searchFolder;
            FolderName.Text = currentFolder.Name;
            MailList.ItemsSource = currentFolder.mails;
            Siderbar.SelectedItem = null;
        }

        private void Reply_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count != 1)
            {
                MessageHandler.AddMessage("Select one mail to reply", MessageSeverity.Error);
                return;
            }
            MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

            var reply = currentFolder.mailHandler.Reply(mailContent);
            (MainFrame.Content as Email).ChangeMail(reply, true);
        }

        private void ReplyAll_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count != 1)
            {
                MessageHandler.AddMessage("Select one mail to reply", MessageSeverity.Error);
                return;
            }
            MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

            var reply = currentFolder.mailHandler.ReplyAll(mailContent);
            (MainFrame.Content as Email).ChangeMail(reply, true);

        }

        private void Forward_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count != 1)
            {
                MessageHandler.AddMessage("Select one mail to forward", MessageSeverity.Error);
                return;
            }
            MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

            var reply = currentFolder.mailHandler.Forward(mailContent);
            (MainFrame.Content as Email).ChangeMail(reply, true);

        }

        private void ReadUnread_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count != 1)
            {
                MessageHandler.AddMessage("Select one mail to mark as unread/read", MessageSeverity.Error);
                return;
            }

            MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

            var reply = currentFolder.mailHandler.UpdateFlag(mailContent, MailFlag.unread);

            currentFolder.mails[MailList.SelectedIndex].Unread = !currentFolder.mails[MailList.SelectedIndex].Unread;
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count != 1)
            {
                MessageHandler.AddMessage("Select a mail to delete it", MessageSeverity.Error);
                return;
            }
            MailContent mailContent = currentFolder.mailsContent[MailList.SelectedIndex];

            var reply = currentFolder.mailHandler.UpdateFlag(mailContent, MailFlag.trash);

        }

        private void MassDelete_Click(object sender, RoutedEventArgs e)
        {
            if (MailList.SelectedItems.Count > 0)
            {
                MessageHandler.AddMessage("Select at least one mail", MessageSeverity.Error);
                return;
            }



        }
    }

    public class SelectedToOpacity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? 1 : 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DateToNiceString : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var date = (DateTime)value;

            if (date.Date == DateTime.Now.Date)
            {
                return date.ToString("t");
            }
            else if (date.Date == DateTime.Now.Date.AddDays(-1))
            {
                return "Yesterday";
            }
            else if (date.Date > DateTime.Now.Date.AddDays(-7))
            {
                return date.ToString("dddd");
            }
            else
            {
                return date.ToString("d");
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageSeverityToInfoBarSeverity : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            InfoBarSeverity severity = InfoBarSeverity.Informational;
            switch (value)
            {
                case MessageSeverity.Info:
                    severity = InfoBarSeverity.Informational;
                    break;
                case MessageSeverity.Success:
                    severity = InfoBarSeverity.Success;
                    break;
                case MessageSeverity.Warning:
                    severity = InfoBarSeverity.Warning;
                    break;
                case MessageSeverity.Error:
                    severity = InfoBarSeverity.Error;
                    break;
            }
            return severity;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class SiderbarTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AccountTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is GmailAccountViewModel)
            {
                return AccountTemplate;
            }
            else if (item is FolderViewModel)
            {
                return FolderTemplate;
            }
            throw new NotImplementedException();
        }
    }


    // Thanks to https://github.com/kaiguo/TreeViewConditionalReorderSample/blob/master/TreeViewConditionalReorderSample/MyTreeViewItem.cs
    class MyTreeViewItem : TreeViewItem
    {
        protected override void OnDragEnter(DragEventArgs e)
        {
            var draggedItem = MainWindow.DraggedItems[0];
            var draggedOverItem = DataContext;
            // Block TreeViewNode auto expanding if we are dragging a group onto another group
            if (draggedItem is AccountViewModel && draggedOverItem is AccountViewModel)
            {
                e.Handled = true;
            }

            base.OnDragEnter(e);
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            var draggedItem = MainWindow.DraggedItems[0];
            var draggedOverItem = DataContext;

            Debug.WriteLine("DraggedItem: " + draggedItem);

            if (draggedItem is AccountViewModel && (draggedOverItem is AccountViewModel || draggedOverItem is FolderViewModel))
            {
                //- Group
                //-- Leaf1
                //-- (Group2) <- Blocks dropping another Group here
                //-- Leaf2
                e.Handled = true;
            }
            base.OnDragOver(e);
            e.AcceptedOperation = draggedOverItem is AccountViewModel && !(draggedItem is AccountViewModel) ? DataPackageOperation.Move : DataPackageOperation.None;
        }
        protected override void OnDrop(DragEventArgs e)
        {
            var data = DataContext as AccountViewModel;
            // Block all drops on leaf node
            if (data == null)
            {
                e.Handled = true;
            }

            base.OnDrop(e);
        }

    }
}
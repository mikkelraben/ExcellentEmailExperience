using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.ViewModel;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
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
        public MainWindow()
        {
            this.InitializeComponent();

            this.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            Titlebar.Loaded += Titlebar_Loaded;

            Title = "EEE";

            this.MinWidth = 760;
            this.MinHeight = 450;

            //if the the app is not in debug mode then collapse the subtitle
#if !DEBUG
            Subtitle.Opacity = 0;
#endif

            var mails = new List<InboxMail> { };
            for (int i = 0; i < 20; i++)
            {
                InboxMail mail = new InboxMail();

                mail.from = new System.Net.Mail.MailAddress("testlongusername@example.org");
                mail.to = [new System.Net.Mail.MailAddress("user@example.org")];
                mail.subject = "This is an awfully long subject line, like just unnecessarily long subject who would want to read this";
                //mail.body = "This is a test email";
                mail.date = DateTime.Now.AddDays(-i).ToString();
                mails.Add(mail);
            }
            MailList.ItemsSource = mails;
            //MainFrame.Content = new Email(mails[0]);
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
                //MainFrame.Content = new Email(MailList.SelectedItem as InboxMail);
                MassEditMenu.Visibility = Visibility.Collapsed;
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
            RefreshButton.Opacity = 0;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

        }

        bool sidebarLarge = false;

        public bool SidebarLarge
        {
            get => sidebarLarge; set
            {
                sidebarLarge = value;
                BackButton.IsEnabled = !value;
                Siderbar.Width = value ? 200 : 0;

            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SidebarLarge)
            {
                SidebarLarge = true;
            }
        }

        private void Folder_Click(object sender, RoutedEventArgs e)
        {
            if (SidebarLarge)
            {
                SidebarLarge = false;
            }
        }

        bool settingsActive = false;
        SettingsWindow settings = new();

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (settingsActive)
            {
                if (settings.Visible)
                    settings.BringToFront();
                return;
            }

            settings = new();

            settings.SetIsMaximizable(false);
            settings.SetIsMinimizable(false);

            settings.MoveAndResize(AppWindow.Position.X + 32, AppWindow.Position.Y + 32, Width - 64, Height - 64);

            settings.Activate();
            settingsActive = true;
            settings.Closed += (s, e) => settingsActive = false;
            Closed += (s, e) => settings.Close();
        }

        private void MailList_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (SidebarLarge)
                SidebarLarge = false;
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
            var date = DateTime.Parse(value as string);

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

    //public static class AncestorSource
    //{
    //    public static readonly DependencyProperty AncestorTypeProperty =
    //        DependencyProperty.RegisterAttached(
    //            "AncestorType",
    //            typeof(Type),
    //            typeof(AncestorSource),
    //            new PropertyMetadata(default(Type), OnAncestorTypeChanged)
    //    );

    //    public static void SetAncestorType(FrameworkElement element, Type value) =>
    //        element.SetValue(AncestorTypeProperty, value);

    //    public static Type GetAncestorType(FrameworkElement element) =>
    //        (Type)element.GetValue(AncestorTypeProperty);

    //    private static void OnAncestorTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    //    {
    //        FrameworkElement target = (FrameworkElement)d;
    //        if (target.IsLoaded)
    //            SetDataContext(target);
    //        else
    //            target.Loaded += OnTargetLoaded;
    //    }

    //    private static void OnTargetLoaded(object sender, RoutedEventArgs e)
    //    {
    //        FrameworkElement target = (FrameworkElement)sender;
    //        target.Loaded -= OnTargetLoaded;
    //        SetDataContext(target);
    //    }

    //    private static void SetDataContext(FrameworkElement target)
    //    {
    //        Type ancestorType = GetAncestorType(target);
    //        if (ancestorType != null)
    //            target.DataContext = FindParent(target, ancestorType);
    //    }

    //    private static object FindParent(DependencyObject dependencyObject, Type ancestorType)
    //    {
    //        DependencyObject parent = VisualTreeHelper.GetParent(dependencyObject);
    //        if (parent == null)
    //            return null;

    //        if (ancestorType.IsInstanceOfType(parent))
    //            return parent;

    //        return FindParent(parent, ancestorType);
    //    }
    //}
}

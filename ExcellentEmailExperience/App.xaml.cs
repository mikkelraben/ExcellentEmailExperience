using ExcellentEmailExperience.Model;
using ExcellentEmailExperience.Views;
using Microsoft.UI.Xaml;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ExcellentEmailExperience
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            mailApp = new MailApp();
            mailApp.Initialize();

            switch (mailApp.settings.theme)
            {
                case Theme.Light:
                    RequestedTheme = ApplicationTheme.Light;
                    break;
                case Theme.Dark:
                    RequestedTheme = ApplicationTheme.Dark;
                    break;
            }


            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (!mailApp.HasAccount())
            {
                intro = new Intro(mailApp);
                intro.FirstAccountCreated += (sender, e) =>
                {
                    CreateMainWindow(mailApp);
                };
                intro.Activate();
            }
            else
            {
                CreateMainWindow(mailApp);
            }

        }

        public void CreateMainWindow(MailApp mailApp)
        {
            mainWindow = new MainWindow(mailApp);
            mainWindow.Activate();
        }

        private Intro? intro;
        public static MainWindow? mainWindow;
        private MailApp? mailApp;
    }
}

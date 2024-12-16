using ExcellentEmailExperience.Model;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Helpers
{
    internal class TitleBarHelper
    {
        public static void ConfigureTitleBar(MailApp mailApp, AppWindowTitleBar titlebar)
        {
            switch (mailApp.settings.theme)
            {
                case Theme.Light:
                    titlebar.ButtonForegroundColor = Colors.Black;
                    break;
                case Theme.Dark:
                    titlebar.ButtonForegroundColor = Colors.White;
                    break;
            }
        }

    }
}

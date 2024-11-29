using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    public enum Theme
    {
        System,
        Light,
        Dark
    }

    public class AppSettings
    {
        public Theme theme;
        public string[] signatures;
        public MessageSeverity logLevel;
        public string mainMail;

    }
}

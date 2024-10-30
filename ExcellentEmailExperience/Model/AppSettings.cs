using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExcellentEmailExperience.Model
{
    enum Theme
    {
        System,
        Light,
        Dark
    }

    public class AppSettings
    {
        Theme theme;
        string[] signatures;

    }
}

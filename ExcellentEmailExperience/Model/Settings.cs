﻿using System;
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

    internal class Settings
    {
        Theme Darkmode;
        string[] signatures;
        // seje settings pending (LLM on and off, spamfilterstuff)

    }
}

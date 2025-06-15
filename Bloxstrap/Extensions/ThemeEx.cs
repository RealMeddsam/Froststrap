﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using Microsoft.Win32;

namespace Bloxstrap.Extensions
{
    public static class ThemeEx
    {
        public static Theme GetFinal(this Theme dialogTheme)
        {
            if (dialogTheme != Theme.Default)
                return dialogTheme;

            using var key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");

            if (key?.GetValue("AppsUseLightTheme") is int value && value == 0)
                return Theme.Dark;

            return Theme.Light;
        }

        public static IReadOnlyCollection<Theme> Selections => new Theme[]
        {
            Theme.Default,
            Theme.Dark,
            Theme.Light,
            Theme.ClassicDark,
            Theme.Froststrap,
            Theme.Midnight,
            Theme.RedBlue,
            Theme.PurpleMoon,
            Theme.Forest,
            Theme.CottonCandy,
            Theme.Purple,
            Theme.Blue,
            Theme.Cyan,
            Theme.Green,
            Theme.Orange,
            Theme.Pink,
            Theme.Red,
        };


        public static Icon GetIcon(this Theme icon)
        {
            return icon switch
            {
                Theme.Default => Properties.Resources.Default,
                Theme.Dark => Properties.Resources.Dark,
                Theme.Light => Properties.Resources.Light,
                Theme.ClassicDark => Properties.Resources.ClassicDark,
                Theme.CottonCandy => Properties.Resources.CottonCandy,
                Theme.Forest => Properties.Resources.Forest,
                Theme.Froststrap => Properties.Resources.Froststrap,
                Theme.Midnight => Properties.Resources.Midnight,
                Theme.PurpleMoon => Properties.Resources.PurpleMoon,
                Theme.RedBlue => Properties.Resources.RedBlue,
                Theme.Purple => Properties.Resources.Purple,
                Theme.Blue => Properties.Resources.Blue,
                Theme.Cyan => Properties.Resources.Cyan,
                Theme.Green => Properties.Resources.Green,
                Theme.Orange => Properties.Resources.Orange,
                Theme.Pink => Properties.Resources.Pink,
                Theme.Red => Properties.Resources.Red,
                _ => Properties.Resources.Default
            };
        }
    }
}

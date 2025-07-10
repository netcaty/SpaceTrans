using Microsoft.Win32;
using System;
using System.Windows.Forms;

namespace SpaceTrans
{
    public static class AutoStartHelper
    {
        private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private static string AppName => Application.ProductName;
        private static string AppPath => Application.ExecutablePath;

        public static bool IsAutoStartEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                var value = key?.GetValue(AppName) as string;
                return value != null && value.Contains(AppPath, StringComparison.OrdinalIgnoreCase);
            }
        }

        public static void SetAutoStart(bool enable)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (enable)
                {
                    key.SetValue(AppName, $"\"{AppPath}\" --tray");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
    }
}
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
namespace HarbingerWatermarkOverlay
{
    internal static class AutoStart
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "HarbingerWatermarkOverlay";

        public static void RegisterForCurrentUser()
        {
            string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName 
                         ?? AppContext.BaseDirectory;
            using var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(RunKeyPath, writable: true) 
                ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            key?.SetValue(AppName, $"\"{exe}\"");
        }

        public static void UnregisterForCurrentUser()
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
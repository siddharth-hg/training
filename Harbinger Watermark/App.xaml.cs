using System;
using System.IO;
using System.Linq;
using System.Windows;

// Alias WinForms so we can still use Screen without colliding with WPF Application.
using Forms = System.Windows.Forms;

namespace HarbingerWatermarkOverlay
{
    // Explicitly inherit the WPF Application
    public partial class App : System.Windows.Application
    {
        // Tweak these defaults as you like
        public const double DefaultOpacity = 0.25;   // 8% visibility
        public const double ScaleOfShortSide = 0.70; // 70% of monitor’s short edge
        public const double RotationAngle = -30.0;   // diagonal; set 0 for centered
        public const bool   Centered = true;         // true = center; false = top-left

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Optional command line to install/remove autostart
            
            if (e.Args.Contains("--install-startup"))
            {
                AutoStart.RegisterForCurrentUser();
                Shutdown();
                return;
            }
            if (e.Args.Contains("--remove-startup"))
            {
                AutoStart.UnregisterForCurrentUser();
                Shutdown();
                return;
            }


            string imgPath = Path.Combine(AppContext.BaseDirectory, "watermark.png");
            if (!File.Exists(imgPath))
            {
                // Use WPF MessageBox (not WinForms) to avoid more ambiguity
                System.Windows.MessageBox.Show(
                    $"watermark.png not found in:\n{AppContext.BaseDirectory}",
                    "HarbingerWatermarkOverlay",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }

            // One overlay window per monitor (still fine to use WinForms Screen)
            foreach (var scr in Forms.Screen.AllScreens)
            {
                var ow = new OverlayWindow(imgPath,
                                           DefaultOpacity,
                                           ScaleOfShortSide,
                                           RotationAngle,
                                           Centered);

                ow.WindowStartupLocation = WindowStartupLocation.Manual;
                ow.Left   = scr.Bounds.Left;
                ow.Top    = scr.Bounds.Top;
                ow.Width  = scr.Bounds.Width;
                ow.Height = scr.Bounds.Height;
                ow.Show();
            }
        }
    }
}

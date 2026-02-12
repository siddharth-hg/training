using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HarbingerWatermarkOverlay
{
    public partial class OverlayWindow : Window
    {
        private readonly string _imgPath;
        private readonly double _opacity;
        private readonly double _scaleOfShortSide;
        private readonly double _rotationAngle;
        private readonly bool   _centered;

        public OverlayWindow(string imgPath, double opacity, double scaleOfShortSide, double rotationAngle, bool centered)
        {
            InitializeComponent();

            _imgPath          = imgPath;
            _opacity          = Math.Clamp(opacity, 0.0, 1.0);
            _scaleOfShortSide = Math.Clamp(scaleOfShortSide, 0.05, 1.5);
            _rotationAngle    = rotationAngle;
            _centered         = centered;

            Loaded += OnLoaded;
            SizeChanged += (_, __) => UpdateLayoutForMonitor();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 1) Apply image (as before)
            ApplyImage();

            // 2) Set device/user text (choose one format)
            string device = Environment.MachineName;
            string user   = Environment.UserName;
            DeviceText.Text = $"{device}  •  {user}".ToUpperInvariant();
            // If you want only device name:
            // DeviceText.Text = Environment.MachineName.ToUpperInvariant();

            // 3) Apply the same rotation to the full visual (image + text)
            if (Math.Abs(_rotationAngle) > double.Epsilon)
            {
                WatermarkVisual.RenderTransform = new RotateTransform(_rotationAngle);
                WatermarkVisual.RenderTransformOrigin = new Point(0.5, 0.5);
            }

            // 4) Keep the window click-through & topmost (as before)
            MakeWindowClickThroughAndToolWindow();

            // 5) Scale & placement (as before)
            UpdateLayoutForMonitor();
        }

        private void ApplyImage()
        {
            var bi = new BitmapImage();
            using var fs = new FileStream(_imgPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            bi.BeginInit();
            bi.CacheOption  = BitmapCacheOption.OnLoad;
            bi.StreamSource = fs;
            bi.EndInit();

            Img.Source  = bi;
            Img.Opacity = _opacity;
        }

        private void UpdateLayoutForMonitor()
        {
            double shortSide = Math.Min(ActualWidth, ActualHeight);
            double target    = shortSide * _scaleOfShortSide;

            Scaler.Width  = target;
            Scaler.Height = target;

            Scaler.HorizontalAlignment = _centered ? HorizontalAlignment.Center : HorizontalAlignment.Left;
            Scaler.VerticalAlignment   = _centered ? VerticalAlignment.Center   : VerticalAlignment.Top;
            if (!_centered) Scaler.Margin = new Thickness(32);
        }

        private void MakeWindowClickThroughAndToolWindow()
        {
            var hwnd = new WindowInteropHelper(this).EnsureHandle();
            var ex = Native.GetWindowLongPtrCompat(hwnd, Native.GWL_EXSTYLE);
            long styles = ex.ToInt64();
            styles |= Native.WS_EX_TRANSPARENT | Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
            Native.SetWindowLongPtrCompat(hwnd, Native.GWL_EXSTYLE, new IntPtr(styles));

            Native.SetWindowPos(hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
        }
    }
}

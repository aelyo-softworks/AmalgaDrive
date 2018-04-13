using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AmalgaDrive.Utilities
{
    public static class UIUtilities
    {
        private static Lazy<Thickness> _windowCaptionHeight = new Lazy<Thickness>(() =>
        {
            // https://stackoverflow.com/questions/28524463/how-to-get-the-default-caption-bar-height-of-a-window-in-windows
            // https://connect.microsoft.com/VisualStudio/feedback/details/763767/the-systemparameters-windowresizeborderthickness-seems-to-return-incorrect-value
            int addedBorder = GetSystemMetrics(SM_CXPADDEDBORDER);
            var tn = SystemParameters.WindowNonClientFrameThickness;
            return new Thickness(tn.Left, tn.Top + addedBorder, tn.Right, tn.Bottom);
        });

        private static Lazy<Thickness> _glassFrameThickness = new Lazy<Thickness>(() =>
        {
            int addedBorder = GetSystemMetrics(SM_CXPADDEDBORDER);
            var tn = SystemParameters.WindowNonClientFrameThickness;
            return new Thickness(2, tn.Top + addedBorder, 2, 2);
        });

        private static Lazy<Thickness> _titleMargin = new Lazy<Thickness>(() =>
        {
            var height = _windowCaptionHeight.Value.Top;
            return new Thickness(8, (height - TitleSize) / 2, 0, 0);
        });

        private static Lazy<ImageSource> _iconSource = new Lazy<ImageSource>(() =>
        {
            using (var stream = Application.GetResourceStream(new Uri("/" + typeof(App).Namespace + ".ico", UriKind.Relative)).Stream)
            {
                using (var icon = new Icon(stream, new System.Drawing.Size(256, 256)))
                {
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, new Int32Rect(0, 0, icon.Width, icon.Height), BitmapSizeOptions.FromEmptyOptions());
                }
            }
        });

        private static Lazy<Icon> _icon = new Lazy<Icon>(() =>
        {
            using (var stream = Application.GetResourceStream(new Uri("/" + typeof(App).Namespace + ".ico", UriKind.Relative)).Stream)
            {
                return new Icon(stream, new System.Drawing.Size(256, 256));
            }
        });

        public static Thickness WindowCaptionHeight => _windowCaptionHeight.Value;
        public static Thickness GlassFrameThickness => _glassFrameThickness.Value;
        public static Thickness TitleMargin => _titleMargin.Value;
        public static double TitleSize => 12;
        public static ImageSource IconSource => _iconSource.Value;
        public static Icon Icon => _icon.Value;

        private const int SM_CXPADDEDBORDER = 0x5C;

        [DllImport("user32")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int WM_NCHITTEST = 0x0084;

        [DllImport("user32")]
        private static extern IntPtr DefWindowProc(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static IntPtr HandleNoResize(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                var ht = DefWindowProc(hWnd, msg, wParam, lParam).ToInt32();
                switch (ht)
                {
                    case HTLEFT:
                    case HTRIGHT:
                    case HTTOP:
                    case HTTOPLEFT:
                    case HTTOPRIGHT:
                    case HTBOTTOM:
                    case HTBOTTOMLEFT:
                    case HTBOTTOMRIGHT:
                        handled = true;
                        return IntPtr.Zero;
                }
            }
            return IntPtr.Zero;
        }

        public static void HookNoResize(this Window window, Func<bool> canResizeFunc = null)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            var source = (HwndSource)PresentationSource.FromVisual(window);
            source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                bool b = canResizeFunc != null ? canResizeFunc() : false;
                if (!b)
                {
                    var ret = HandleNoResize(hwnd, msg, wParam, lParam, ref handled);
                    if (handled)
                        return ret;
                }

                return IntPtr.Zero;
            });
        }

        public static void OpenExplorer(string directoryPath)
        {
            if (directoryPath == null)
                throw new ArgumentNullException(nameof(directoryPath));

            // see http://support.microsoft.com/kb/152457/en-us
            Process.Start("explorer.exe", "/e,/root,/select," + directoryPath);
        }

        public static string GetProduct() => Assembly.GetEntryAssembly().GetProduct();
        public static string GetProduct(this Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            object[] atts = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            if (atts != null && atts.Length > 0)
                return ((AssemblyProductAttribute)atts[0]).Product;

            return null;
        }

        public static Window GetActiveWindow()
        {
            var app = Application.Current;
            if (app == null)
                return null;

            return app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
        }

        public static void ShowError(this Window window, string text) => ShowMessage(window, text, MessageBoxImage.Error);

        public static void ShowMessage(this Window window, string text) => MessageBox.Show(window, text, GetProduct(), MessageBoxButton.OK);
        public static void ShowMessage(this Window window, string text, MessageBoxImage image)
        {
            window = window ?? GetActiveWindow();
            MessageBox.Show(window, text, GetProduct(), MessageBoxButton.OK, image);
        }

        public static MessageBoxResult ShowConfirm(this Window window, string text)
        {
            window = window ?? GetActiveWindow();
            return MessageBox.Show(window, text, GetProduct(), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        }

        public static MessageBoxResult ShowConfirmCancel(this Window window, string text)
        {
            window = window ?? GetActiveWindow();
            return MessageBox.Show(window, text, GetProduct(), MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        }

        public static T GetDataContext<T>(object input)
        {
            if (input == null)
                return default(T);

            var fe = input as FrameworkElement;
            if (fe == null || fe.DataContext == null)
                return default(T);

            if (!typeof(T).IsAssignableFrom(fe.DataContext.GetType()))
                return default(T);

            return (T)fe.DataContext;
        }
    }
}

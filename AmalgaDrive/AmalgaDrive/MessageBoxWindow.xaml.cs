using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using AmalgaDrive.Utilities;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive
{
    public partial class MessageBoxWindow : Window
    {
        public MessageBoxWindow()
        {
            InitializeComponent();
            DependencyPropertyDescriptor.FromProperty(TitleProperty, typeof(Window)).AddValueChanged(this, (sender, args) => ThisTitle.Text = Title);
            Title = UIUtilities.GetProduct();
            DialogResultForButton1 = true;
            DialogResultForButton2 = false;
            ResultForButton1 = MessageBoxResult.OK;
            ResultForButton2 = MessageBoxResult.Cancel;
            Button2.Visibility = Visibility.Collapsed;
            Button3.Visibility = Visibility.Collapsed;
            Button4.Visibility = Visibility.Collapsed;
            Sep1.Visibility = Visibility.Collapsed;
            Sep2.Visibility = Visibility.Collapsed;
            Sep3.Visibility = Visibility.Collapsed;
            Loaded += OnLoaded;
        }

        public bool CanUserResize { get; set; }
        public bool? DialogResultForButton1 { get; set; }
        public bool? DialogResultForButton2 { get; set; }
        public bool? DialogResultForButton3 { get; set; }
        public bool? DialogResultForButton4 { get; set; }
        public MessageBoxResult ResultForButton1 { get; set; }
        public MessageBoxResult ResultForButton2 { get; set; }
        public MessageBoxResult ResultForButton3 { get; set; }
        public MessageBoxResult ResultForButton4 { get; set; }
        public MessageBoxResult Result { get; private set; }

        private void OnLoaded(object sender, RoutedEventArgs e) { MinWidth = ActualWidth; MinHeight = ActualHeight; }
        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        protected override void OnSourceInitialized(EventArgs e) => this.HookNoResize(() => CanUserResize);

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Result = MessageBoxResult.Cancel;
            DialogResult = false;
            Close();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultForButton1;
            DialogResult = DialogResultForButton1;
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultForButton2;
            DialogResult = DialogResultForButton2;
            Close();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultForButton3;
            DialogResult = DialogResultForButton3;
            Close();
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            Result = ResultForButton4;
            DialogResult = DialogResultForButton4;
            Close();
        }
    }

    public static class MessageBoxWindowExtensions
    {
        public static MessageBoxResult Show(this Window window, string text, string caption,
            MessageBoxButton button = MessageBoxButton.OK,
            MessageBoxImage image = MessageBoxImage.None,
            ImageSource imageSource = null)
        {
            var dlg = new MessageBoxWindow();
            var icon = (StockIconId)0;
            switch (image)
            {
                case MessageBoxImage.Error:
                    icon = StockIconId.ERROR;
                    break;

                case MessageBoxImage.Information:
                    icon = StockIconId.INFO;
                    break;

                case MessageBoxImage.Exclamation:
                    icon = StockIconId.WARNING;
                    break;

                case MessageBoxImage.Question:
                    icon = StockIconId.HELP;
                    break;
            }

            if (icon != 0 || imageSource != null)
            {
                var img = new Image();
                img.VerticalAlignment = VerticalAlignment.Top;
                img.Source = imageSource ?? StockIcon.GetStockBitmap(icon, StockIcon.SHGSI.SHGSI_LARGEICON);
                img.Width = 32;
                img.Margin = new Thickness(0, 0, 10, 0);
                dlg.ContentPanel.Children.Add(img);
            }

            var tb = new TextBlock();
            tb.Text = text;
            tb.MaxWidth = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(window).Handle).WorkingArea.Width / 3;
            tb.TextWrapping = TextWrapping.Wrap;
            dlg.ContentPanel.Children.Add(tb);
            dlg.Title = caption ?? UIUtilities.GetProduct();
            dlg.Owner = window ?? UIUtilities.GetActiveWindow();
            switch (button)
            {
                case MessageBoxButton.OK:
                    dlg.Button1.Content = "OK";
                    dlg.ResultForButton1 = MessageBoxResult.OK;
                    break;

                case MessageBoxButton.OKCancel:
                    dlg.Button1.Content = "OK";
                    dlg.ResultForButton1 = MessageBoxResult.OK;
                    dlg.Button2.Content = "Cancel";
                    dlg.Button1.IsDefault = false;
                    dlg.Button2.IsDefault = true;
                    dlg.ResultForButton1 = MessageBoxResult.Cancel;
                    dlg.Button2.Visibility = Visibility.Visible;
                    dlg.Sep1.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNo:
                    dlg.Button1.Content = "Yes";
                    dlg.ResultForButton1 = MessageBoxResult.Yes;
                    dlg.Button2.Content = "No";
                    dlg.Button1.IsDefault = false;
                    dlg.Button2.IsDefault = true;
                    dlg.ResultForButton2 = MessageBoxResult.No;
                    dlg.Button2.Visibility = Visibility.Visible;
                    dlg.Sep1.Visibility = Visibility.Visible;
                    break;

                case MessageBoxButton.YesNoCancel:
                    dlg.Button1.Content = "Yes";
                    dlg.ResultForButton1 = MessageBoxResult.Yes;
                    dlg.Button2.Content = "No";
                    dlg.ResultForButton2 = MessageBoxResult.No;
                    dlg.Button3.Content = "Cancel";
                    dlg.Button1.IsDefault = false;
                    dlg.Button2.IsDefault = false;
                    dlg.Button3.IsDefault = true;
                    dlg.ResultForButton3 = MessageBoxResult.Cancel;
                    dlg.Button2.Visibility = Visibility.Visible;
                    dlg.Button3.Visibility = Visibility.Visible;
                    dlg.Sep1.Visibility = Visibility.Visible;
                    dlg.Sep2.Visibility = Visibility.Visible;
                    break;
            }

            _ = dlg.ShowDialog();
            return dlg.Result;
        }

        public static void ShowError(this Window window, string text) => ShowMessage(window, text, MessageBoxImage.Error);
        public static void ShowMessage(this Window window, string text) => Show(window, text, null, MessageBoxButton.OK);
        public static void ShowMessage(this Window window, string text, MessageBoxImage image) => Show(window, text, null, MessageBoxButton.OK, image);
        public static void ShowMessage(this Window window, string text, ImageSource image) => Show(window, text, null, MessageBoxButton.OK, MessageBoxImage.None, image);
        public static MessageBoxResult ShowConfirm(this Window window, string text) => Show(window, text, null, MessageBoxButton.YesNo, MessageBoxImage.Question);
        public static MessageBoxResult ShowConfirmCancel(this Window window, string text) => Show(window, text, null, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
    }
}

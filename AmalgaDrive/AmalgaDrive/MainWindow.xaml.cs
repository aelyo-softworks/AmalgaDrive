using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using AmalgaDrive.Configuration;
using AmalgaDrive.Model;
using AmalgaDrive.Utilities;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive
{
    public partial class MainWindow : Window
    {
        private HwndSource _source;
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private State _state;

        public MainWindow()
        {
            InitializeComponent();

            // NOTE: icon resource must be named same as namespace + icon
            Icon = UIUtilities.IconSource;
            _notifyIcon.Icon = UIUtilities.Icon;
            _notifyIcon.Text = Assembly.GetEntryAssembly().GetTitle();
            _notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu();
            _notifyIcon.ContextMenu.MenuItems.Add("Show", Show);
            _notifyIcon.ContextMenu.MenuItems.Add("-");
            _notifyIcon.ContextMenu.MenuItems.Add("Quit", Close);
            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick += Show;

            _state = new State(this);
            DataContext = _state;
            ReloadItems();
        }

        private class State : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            public State(MainWindow window)
            {
                Window = window;
                window.StateChanged += StateChanged;
            }

            public MainWindow Window { get; }
            public Visibility MaximizeVisibility => Window.WindowState == WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
            public Visibility RestoreVisibility => Window.WindowState == WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;

            public void StateChanged(object sender, EventArgs args)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaximizeVisibility)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RestoreVisibility)));
            }
        }

        private void ReloadItems()
        {
            Drives.ItemsSource = Settings.Current.DriveServices.Select(s => new DriveService(s));
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // this handles the singleton instance
            _source = (HwndSource)PresentationSource.FromVisual(this);
            _source.AddHook((IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) =>
            {
                if (msg == Program.Singleton.Message && WindowState == WindowState.Minimized)
                {
                    Show(null, null);
                    handled = true;
                    return IntPtr.Zero;
                }

                var ret = Program.Singleton.OnWndProc(hwnd, msg, wParam, lParam, true, true, ref handled);
                if (handled)
                    return ret;

                return ret;
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _notifyIcon?.Dispose();
        }

        // hide if manually minimized
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        // minimized if closed
        protected override void OnClosing(CancelEventArgs e)
        {
#if DEBUG
            base.OnClosing(e);
            return;
#else
            if (WindowState == WindowState.Minimized || // if CTRL+SHIFT is pressed, do close
                ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift))
            {
                base.OnClosing(e);
                return;
            }

            e.Cancel = true;
            base.OnClosing(e);
            WindowState = WindowState.Minimized;
#endif
        }

        private void Close(object sender, EventArgs e)
        {
            WindowState = WindowState.Minimized;
            Close();
        }

        private void Show(object sender, EventArgs e)
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void FileExit_Click(object sender, RoutedEventArgs e) => Close(null, null);
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Maximized;
        private void Minimize_Click(object sender, RoutedEventArgs e) => Close(null, null);
        private void Restore_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Normal;

        private void Options_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DockPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => DragMove();

        private void HelpAbout_Click(object sender, RoutedEventArgs e) => this.ShowMessage("Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks" + Environment.NewLine + "All rights reserved.", UIUtilities.IconSource);

        private void AddDrive_Click(object sender, RoutedEventArgs e)
        {
            var service = new DriveService();
            var dlg = new EditDriveServiceWindow(service);
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                ReloadItems();
            }
        }

        private void OpenConfig_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(Settings.DefaultConfigurationFilePath))
            {
                UIUtilities.OpenExplorer(Path.GetDirectoryName(Settings.DefaultConfigurationFilePath));
            }
            else
            {
                this.ShowMessage("Nothing is configured yet.", MessageBoxImage.Information);
            }
        }

        private void DeleteService_Click(object sender, RoutedEventArgs e)
        {
            var service = UIUtilities.GetDataContext<DriveService>(sender);
            if (this.ShowConfirm("Are you sure you want to remove the '" + service.Name + "' service?") != MessageBoxResult.Yes)
                return;

            Settings.Current.RemoveDriveService(service.Name);
            ReloadItems();
        }

        private void EditService(DriveService service)
        {
            if (service == null)
                return;

            var dlg = new EditDriveServiceWindow(service);
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                ReloadItems();
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e) => EditService(UIUtilities.GetDataContext<DriveService>(sender));
        private void Drives_MouseDoubleClick(object sender, MouseButtonEventArgs e) => EditService(UIUtilities.GetDataContext<DriveService>(e.OriginalSource));
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using AmalgaDrive.Configuration;
using AmalgaDrive.Folder;
using AmalgaDrive.Model;
using AmalgaDrive.Utilities;
using ShellBoost.Core;
using ShellBoost.Core.Utilities;
using ShellBoost.Core.WindowsShell;

namespace AmalgaDrive
{
    public partial class MainWindow : Window
    {
        private HwndSource _source;
        private Thread _serverThread;
        private AutoResetEvent _serverStopEvent;
        private System.Windows.Forms.NotifyIcon _notifyIcon = new System.Windows.Forms.NotifyIcon();
        private LogsWindow _logs;
        private State _state;
        private ObservableCollection<DriveService> _driveServices = new ObservableCollection<DriveService>();

        public MainWindow()
        {
            InitializeComponent();

            _logs = new LogsWindow();

            AppendText("AmalgaDrive " + (IntPtr.Size == 8 ? "64" : "32") + "bit - V" + Assembly.GetExecutingAssembly().GetInformationalVersion() + " - Copyright © 2017-" + DateTime.Now.Year + " Aelyo Softworks. All rights reserved.");
            AppendText();

            _serverStopEvent = new AutoResetEvent(false);
            _serverThread = new Thread(DriveThread);
            _serverThread.IsBackground = true;
            _serverThread.Start();

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
            Drives.ItemsSource = _driveServices;

            foreach (var drive in Settings.Current.DriveServiceSettings)
            {
                _driveServices.Add(new DriveService(drive));
            }
        }

        private void DriveThread(object state)
        {
            var logger = new Logger(this);
            var config = new ShellFolderConfiguration();
            config.Logger = logger;

            do
            {
                try
                {
                    ShellFolderServer.RegisterNativeDll(RegistrationMode.User);
                    ShellUtilities.RefreshShellViews();

                    using (var server = new OnDemandShellFolderServer(new DirectoryInfo(DriveService.AllRootsPath)))
                    {
                        server.Start(config);
                        AppendText("Started listening on proxy id " + ShellFolderServer.ProxyId);
                        _serverStopEvent.WaitOne();
                        return;
                    }
                }
                catch (Exception e)
                {
                    logger.Log(TraceLevel.Error, "An error occurred: " + e);
                    Thread.Sleep(1000);
                }
            }
            while (true);
        }

        private class Logger : ILogger
        {
            private MainWindow _window;

            public Logger(MainWindow window)
            {
                _window = window;
            }

            public void Log(TraceLevel level, object value, [CallerMemberName] string methodName = null) => _window.AppendTrace(level, value, methodName);
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

        public void AppendTrace(TraceLevel level, object value, [CallerMemberName] string methodName = null) => AppendText("[" + level + "]" + methodName + ": " + value);
        public void AppendText() => AppendText(null);
        public void AppendText(string text)
        {
            if (text != null)
            {
                text = DateTime.Now + " [" + Thread.CurrentThread.ManagedThreadId + "]: " + text;
            }

            Dispatcher.BeginInvoke(() =>
            {
                _logs.TB.Text += Environment.NewLine;
                if (text != null)
                {
                    _logs.TB.Text += text;
                }
                _logs.TB.ScrollToEnd();
            });
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
            ShellFolderServer.UnregisterNativeDll(RegistrationMode.User);
            ShellUtilities.RefreshShellViews();

            if (_serverStopEvent != null)
            {
                _serverStopEvent.Set();
                _serverStopEvent.Dispose();
            }

            var thread = _serverThread;
            if (thread != null)
            {
                thread.Join(1000);
            }
            _notifyIcon?.Dispose();

            _logs.Hide();
            _logs.Close();
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

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void HelpAbout_Click(object sender, RoutedEventArgs e) => this.ShowMessage("Copyright (C) 2017-" + DateTime.Now.Year + " Aelyo Softworks" + Environment.NewLine + "All rights reserved.", UIUtilities.IconSource);

        private void AddDrive_Click(object sender, RoutedEventArgs e)
        {
            var service = new DriveService();
            var dlg = new EditDriveServiceWindow(service);
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                _driveServices.Add(service);
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
            if (this.ShowConfirm("Are you sure you want to remove the '" + service.Name + "' service? Note it can take some time if the number of synchronized files is important.") != MessageBoxResult.Yes)
                return;

            Settings.Current.RemoveDriveService(service);
            _driveServices.Remove(service);
        }

        private void EditService(DriveService service)
        {
            if (service == null)
                return;

            var dlg = new EditDriveServiceWindow(service);
            dlg.Owner = this;
            if (dlg.ShowDialog().GetValueOrDefault())
            {
                // do nothing
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e) => EditService(UIUtilities.GetDataContext<DriveService>(sender));
        private void Drives_MouseDoubleClick(object sender, MouseButtonEventArgs e) => EditService(UIUtilities.GetDataContext<DriveService>(e.OriginalSource));
        private void OpenPath_Click(object sender, RoutedEventArgs e) => WindowsUtilities.OpenExplorer(UIUtilities.GetDataContext<DriveService>(sender).RootPath);

        private void OpenExtension_Click(object sender, RoutedEventArgs e)
        {
            var id = ShellFolderServer.LocationFolderId;
            var kn = KnownFolder.Get(id);
            var idl = kn.GetIdList(KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT);

            dynamic window = new ShellUtilities.ShellBrowserWindow();
            ShellUtilities.CoAllowSetForegroundWindow(window);
            window.Visible = true;
            window.Navigate2(idl.Data);
        }

        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            if (_logs.Visibility == Visibility.Visible)
            {
                _logs.Hide();
            }
            else
            {
                _logs.Show();
            }
        }

        private void RestartExplorer_Click(object sender, RoutedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                var rm = new RestartManager();
                rm.RestartExplorerProcesses((s) =>
                {
                    AppendText("Windows Explorer was stopped...");
                }, false, out Exception error);
                AppendText("Windows Explorer was restarted...");
            });
        }
    }
}

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using AmalgaDrive.Configuration;
using AmalgaDrive.Drive;
using AmalgaDrive.Model;
using AmalgaDrive.Utilities;

namespace AmalgaDrive
{
    public partial class EditDriveServiceWindow : Window
    {
        private readonly string _dummyPassword;

        public EditDriveServiceWindow(DriveService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            IsNew = string.IsNullOrEmpty(service.Name);
            Service = service;
            DataContext = service;
            InitializeComponent();
            DependencyPropertyDescriptor.FromProperty(TitleProperty, typeof(Window)).AddValueChanged(this, (sender, args) => ThisTitle.Text = Title);
            ServiceType.ItemsSource = DriveServiceDescriptor.ScanDescriptors();
            ServicePassword.IsInactiveSelectionHighlightEnabled = true;

            Title = string.IsNullOrWhiteSpace(service.Name) ? "Add a Cloud Drive" : "'" + service.Name + "' Cloud Drive";

            if (service.Password != null && service.Password.Length > 0)
            {
                _dummyPassword = new string((char)0xFFFF, 8);
                ServicePassword.Password = _dummyPassword;
            }
        }

        public bool IsNew { get; }
        public DriveService Service { get; }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void ServicePassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_dummyPassword != null && ServicePassword.SecurePassword.EqualsOrdinal(_dummyPassword))
                return;

            Service.Password = ServicePassword.SecurePassword;
        }

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
            DialogResult = false;
            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Current.SetDriveService(Service);
            DialogResult = true;
            Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.ShowConfirm("Are you sure you want to reset the '" + Service.Name + "' service? Note it can take some time if the number of synchronized files is important.") != MessageBoxResult.Yes)
                return;

            var period = Service.SyncPeriod;
            Service.SyncPeriod = 0;

            int i = 0;
            int max = 100;
            while (Service.OnDemandSynchronizer.IsSynchronizing && i < max)
            {
                Thread.Sleep(100);
                i++;
            }
            Service.ResetOnDemandSynchronizer();

            if (i >= max)
            {
                this.ShowMessage("The directory is locked for now. Please retry later.");
                return;
            }

            Service.Unregister();
            try
            {
                //Directory.Delete(Service.RootPath, true);
            }
            catch(Exception ex)
            {
                ((App)Application.Current).Log(TraceLevel.Error, "An error occurred trying to delete '" + Service.RootPath  + "' directory: " + ex);
            }
            Service.SyncPeriod = period;
        }
    }
}

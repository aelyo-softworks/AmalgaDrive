using System;
using System.ComponentModel;
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
        private string _dummyPassword;

        public EditDriveServiceWindow(DriveService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            Service = service;
            DataContext = service;
            InitializeComponent();
            DependencyPropertyDescriptor.FromProperty(TitleProperty, typeof(Window)).AddValueChanged(this, (sender, args) => ThisTitle.Text = Title);
            ServiceType.ItemsSource = DriveServiceDescriptor.ScanDescriptors();
            ServicePassword.IsInactiveSelectionHighlightEnabled = true;

            Title = string.IsNullOrWhiteSpace(service.Name) ? "Add a Cloud Service" : "'" + service.Name + "' Cloud Service";

            if (service.Password != null && service.Password.Length > 0)
            {
                _dummyPassword = new string((char)0xFFFF, 8);
                ServicePassword.Password = _dummyPassword;
            }
        }

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
            var setting = new DriveServiceSettings(Service);
            Settings.Current.SetDriveService(setting);
            DialogResult = true;
            Close();
        }
    }
}

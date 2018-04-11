using System.Windows;
using System.Windows.Input;

namespace AmalgaDrive
{
    public partial class EditDriveServiceWindow : Window
    {
        public EditDriveServiceWindow()
        {
            InitializeComponent();
        }

        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

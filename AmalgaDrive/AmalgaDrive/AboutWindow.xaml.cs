using System.Windows;
using System.Windows.Input;

namespace AmalgaDrive
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            Icon = AppParameters.IconSource;
            //ThisIcon.Source = AppParameters.IconSource;
        }

        private void Button_Click(object sender, RoutedEventArgs e) => Close();
        private void DockPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}

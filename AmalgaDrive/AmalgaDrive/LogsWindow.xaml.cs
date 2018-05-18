using System.ComponentModel;
using System.Windows;

namespace AmalgaDrive
{
    public partial class LogsWindow : Window
    {
        public LogsWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                e.Cancel = true;
                Hide();
                return;
            }
            base.OnClosing(e);
        }

        private void Clear_Click(object sender, RoutedEventArgs e) => TB.Clear();
    }
}

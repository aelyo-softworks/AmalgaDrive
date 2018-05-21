using AmalgaDrive.Utilities;

namespace AmalgaDrive
{
    public class SyncPeriodTextBox : ValidatingTextBox
    {
        protected override void OnValidateText(object sender, ValidateTextEventArgs e) => e.Cancel = !int.TryParse(e.Text, out var value) || value < 0;
    }
}

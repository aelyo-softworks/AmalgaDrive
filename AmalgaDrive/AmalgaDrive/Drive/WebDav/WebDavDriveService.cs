using System.ComponentModel;
using System.Windows.Media;
using ShellBoost.Core.Utilities;

namespace AmalgaDrive.Drive.WebDav
{
    [DisplayName("WebDav")]
    public class WebDavDriveService : IDriveService
    {
        public ImageSource Icon => StockIcon.GetStockBitmap(StockIconId.MYNETWORK, StockIcon.SHGSI.SHGSI_LARGEICON);
    }
}
